namespace Services.Redmine
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using NLog;

    using Common.Tasks;
    using Framework.Common;

    using RedmineApi.Core;
    using RedmineApi.Core.Types;

    using Tasks;

    using ProjectRM = RedmineApi.Core.Types.Project;
    using IssueRM = RedmineApi.Core.Types.Issue;
    using IssueStatusRM = RedmineApi.Core.Types.IssueStatus;

    public class RedmineService : IRedmineService, IRedmineVisitor, IDisposable
    {
        #region Classes

        public class QueueItem<T>
        {
            public T Current { get; }

            public T Next { get; }

            public QueueItem(T current, T next = default(T)) 
            {
                Current = current;
                Next = next;
            }
        }

        #endregion Classes

        #region Fields

        private ILogger _logger;
        private IRedmineStrategy _strategy;
        private IRedmineOptions _options;
        private RedmineManager _manager;
        private Dictionary<int, ProjectRM> _projects;
        private Dictionary<int, IssueRM> _issues;
        private Dictionary<int, QueueItem<IssueStatusRM>> _statuses;
        private ITaskQueue<IRedmineVisitor> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Properties

        public IRedmineOptions Options => _options;

        #endregion Properties

        #region Constructors

        public RedmineService(IRedmineStrategy strategy, IRedmineOptions options, ITimelineEnviroment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _options = options;
            _strategy = strategy;
            _strategy.Register(this);

            _projects = new Dictionary<int, ProjectRM>();
            _issues = new Dictionary<int, IssueRM>();
            _statuses = new Dictionary<int, QueueItem<IssueStatusRM>>();

            _cancellationSource = new CancellationTokenSource();
            _queue = new TaskQueue<IRedmineVisitor>(task => task.Handle(this), timeline);
            _queue.Error += (task, error) => _logger?.Error($"failed task: {task}, error: `{error}`"); ;
        }

        public void Dispose()
        {
            Stop();
            _manager?.Dispose();
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            if (_queue.HasEnabled())
                return;

            _manager = _manager ?? new RedmineManager(_options.Host, _options.ApiKey, MimeType.Xml, DefaultRedmineHttpSettings.Create());
            _queue.Start();

            Enqueue(new SyncActionTask<IRedmineVisitor>(SyncProjects));
            Enqueue(new SyncActionTask<IRedmineVisitor>(SyncStatuses));
            Enqueue(new SyncActionTask<IRedmineVisitor>(SyncIssues, _queue, _options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem<IRedmineVisitor> task)
        {
            _queue.Enqueue(task);
        }

        public bool Handle(IUpdateWorkTimeTask task)
        {
            if (!_issues.TryGetValue(task.IssueId, out var issue))
                return false;

            var hours = 
                task.Hours < _options.EstimatedHoursLowerLimit
                    ? _options.EstimatedHoursLowerLimit 
                    : decimal.Round(task.Hours, 1);

            if (hours == 0)
                return false;

            Task.Run(() => _manager.Create(new TimeEntry()
            {
                Issue = new IdentifiableName() { Id = issue.Id },
                Project = new IdentifiableName() { Id = issue.Project.Id },
                Hours = hours,
                Comments = task.Comments,
            }),
            _cancellationSource.Token).Wait();

            return true;
        }

        public bool Handle(IUpdateIssueStatusTask task)
        {
            IssueRM issue = Task.Run(() => _manager.Get<IssueRM>(task.IssueId.ToString(), null), _cancellationSource.Token).Result;

            var statusId = task.StatusId;
            // TODO: Продумать как лучше назначать "следующий" статус.
            if (statusId == -1)
            {
                if (!_statuses.TryGetValue(issue.Status.Id, out var status) || status.Next == null)
                {
                    return false;
                }

                statusId = status.Next.Id;
            }

            // TODO Add script for redmine actions on change status.
            issue.Status = new IssueStatusRM() { Id = statusId };

            if (issue.EstimatedHours == null)
                issue.EstimatedHours = _options.EstimatedHoursABS;

            issue = Task.Run(() => _manager.Update(task.IssueId.ToString(), issue), _cancellationSource.Token).Result;

            _issues[issue.Id] = issue;

            // TODO Add equals property issue.
            return true;
        }

        private T[] GetListAll<T>(Func<T, bool> predicate) where T : class, new()
        {
            List<T> list = Task.Run(() => 
                _manager.ListAll<T>(
                    new NameValueCollection()
                    {
                        { RedmineKeys.ASSIGNED_TO_ID, _options.Sync.UserId.ToString() },
                    }),
                _cancellationSource.Token).Result;

            T[] updates = list
                .Where(predicate)
                .ToArray();

            return updates;
        }

        private bool SyncIssues()
        {
            IssueRM[] updates = GetListAll<IssueRM>(issue => !_issues.ContainsKey(issue.Id) || !_issues[issue.Id].Status.Equals(issue.Status));

            foreach (IssueRM issue in updates)
                _issues[issue.Id] = issue;

            if (updates.Any())
                _strategy.UpdateIssues(updates.Select(s => new Issue(s)).ToArray());

            return true;
        }

        private bool SyncStatuses()
        {
            IssueStatusRM[] updates = GetListAll<IssueStatusRM>(status => !_statuses.ContainsKey(status.Id) || !_statuses[status.Id].Equals(status));

            for (int i = 0; i < updates.Length; i++)
            {
                _statuses[updates[i].Id] = new QueueItem<IssueStatusRM>(updates[i], i < updates.Length - 1 ? updates[i + 1] : null);
            }

            if (updates.Any())
                _strategy.UpdateStatuses(updates.Select(s => new IssueStatus(s)).ToArray());

            return true;
        }

        private bool SyncProjects()
        {
            ProjectRM[] updates = GetListAll<ProjectRM>(project => !_projects.ContainsKey(project.Id) || !_projects[project.Id].Equals(project));

            foreach (ProjectRM item in updates)
                _projects[item.Id] = item;

            if (updates.Any())
                _strategy.UpdateProjects(updates.Select(s => new Project(s)).ToArray());

            return true;
        }

        #endregion Methods
    }
}
