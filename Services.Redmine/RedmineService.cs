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

    public class RedmineService : IRedmineService, IDisposable
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
        private IRedmineOptions _options;
        private RedmineManager _manager;
        private Dictionary<int, Project> _projects;
        private Dictionary<int, Issue> _issues;
        private Dictionary<int, QueueItem<IssueStatus>> _statuses;
        private ITaskQueue<IRedmineService> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<Project[]> UpdateProjects;
        public event EventHandler<Issue[]> UpdateIssues;
        public event EventHandler<IssueStatus[]> UpdateStatuses;

        #endregion Events

        #region Constructors

        public RedmineService(IRedmineOptions options, ITimelineEnviroment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _projects = new Dictionary<int, Project>();
            _issues = new Dictionary<int, Issue>();
            _statuses = new Dictionary<int, QueueItem<IssueStatus>>();

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<IRedmineService>(task => task.Handle(this), timeline);
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

            Enqueue(new SyncActionTask<IRedmineService>(SyncProjects));
            Enqueue(new SyncActionTask<IRedmineService>(SyncStatuses));
            Enqueue(new SyncActionTask<IRedmineService>(SyncIssues, _queue, _options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem<IRedmineService> task)
        {
            _queue.Enqueue(task);
        }

        public bool Handle(IUpdateWorkTimeTask task)
        {
            if (!_issues.ContainsKey(task.IssueId))
                return false;

            var hours = 
                task.Hours < _options.EstimatedHoursLowerLimit
                    ? _options.EstimatedHoursLowerLimit 
                    : decimal.Round(task.Hours, 1);

            if (hours == 0)
                return false;

            Task.Run(() => _manager.Create(new TimeEntry()
            {
                Issue = new IdentifiableName() { Id = _issues[task.IssueId].Id },
                Project = _issues[task.IssueId].Project,
                Hours = hours,
                Comments = task.Comments,
            }),
            _cancellationSource.Token).Wait();

            return true;
        }

        public bool Handle(IUpdateIssueStatusTask task)
        {
            Issue issue = Task.Run(() => _manager.Get<Issue>(task.IssueId.ToString(), null), _cancellationSource.Token).Result;

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
            issue.Status = new IssueStatus() { Id = statusId };

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
            Issue[] updates = GetListAll<Issue>(issue => !_issues.ContainsKey(issue.Id) || !_issues[issue.Id].Status.Equals(issue.Status));

            foreach (Issue issue in updates)
                _issues[issue.Id] = issue;

            if (updates.Any())
                UpdateIssues?.Invoke(this, updates);

            return true;
        }

        private bool SyncStatuses()
        {
            IssueStatus[] updates = GetListAll<IssueStatus>(status => !_statuses.ContainsKey(status.Id) || !_statuses[status.Id].Equals(status));

            for (int i = 0; i < updates.Length; i++)
            {
                _statuses[updates[i].Id] = new QueueItem<IssueStatus>(updates[i], i < updates.Length - 1 ? updates[i + 1] : null);
            }

            if (updates.Any())
                UpdateStatuses?.Invoke(this, updates.ToArray());

            return true;
        }

        private bool SyncProjects()
        {
            Project[] updates = GetListAll<Project>(project => !_projects.ContainsKey(project.Id) || !_projects[project.Id].Equals(project));

            foreach (Project item in updates)
                _projects[item.Id] = item;

            if (updates.Any())
                UpdateProjects?.Invoke(this, updates.ToArray());

            return true;
        }

        #endregion Methods
    }
}
