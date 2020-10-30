namespace Services.Redmine
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using NLog;

    using Framework.Timeline;

    using RedmineApi.Core;
    using RedmineApi.Core.Types;

    using Tasker.Common.Task;
    using Tasker.Interfaces.Task;

    using ProjectRM = RedmineApi.Core.Types.Project;
    using IssueRM = RedmineApi.Core.Types.Issue;
    using IssueStatusRM = RedmineApi.Core.Types.IssueStatus;

    public class RedmineService : ITaskService, ITaskVisitor, IDisposable
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
        private RedmineManager _manager;
        private Dictionary<int, ProjectRM> _projects;
        private Dictionary<int, IssueRM> _issues;
        private Dictionary<int, QueueItem<IssueStatusRM>> _statuses;
        private ITaskQueue _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event Action<object, ITaskCommon> Notify;

        #endregion Events

        #region Properties

        public IRedmineOptions Options { get; }

        #endregion Properties

        #region Constructors

        public RedmineService(IRedmineOptions options, ITimelineEnvironment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            Options = options;

            _projects = new Dictionary<int, ProjectRM>();
            _issues = new Dictionary<int, IssueRM>();
            _statuses = new Dictionary<int, QueueItem<IssueStatusRM>>();

            _cancellationSource = new CancellationTokenSource();
            _queue = new TaskQueue(task => task.Handle(this), timeline);
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

            _manager ??= new RedmineManager(Options.Host, Options.ApiKey, MimeType.Xml, DefaultRedmineHttpSettings.Create());
            _queue.Start();

            Enqueue(new SyncActionTask(SyncProjects));
            Enqueue(new SyncActionTask(SyncStatuses));
            Enqueue(new SyncActionTask(SyncIssues, _queue, Options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem task)
        {
            _queue.Enqueue(task);
        }

        public string Handle(IUpdateTask task)
        {
            IssueRM issue = Task.Run(() => _manager.Get<IssueRM>(task.Id.ToString(), null), _cancellationSource.Token).Result;

            //var statusId = task.StatusId;
            //// TODO: Продумать как лучше назначать "следующий" статус.
            //if (statusId == -1)
            //{
            //    if (!_statuses.TryGetValue(issue.Status.Id, out var status) || status.Next == null)
            //    {
            //        return string.Empty;
            //    }

            //    statusId = status.Next.Id;
            //}

            //// TODO Add script for redmine actions on change status.
            //issue.Status = new IssueStatusRM { Id = statusId };

            //if (issue.EstimatedHours == null)
            //    issue.EstimatedHours = Options.EstimatedHoursABS;

            issue = Task.Run(() => _manager.Update(task.Id.ToString(), issue), _cancellationSource.Token).Result;

            _issues[issue.Id] = issue;

            // TODO Add equals property issue.
            return issue.Id.ToString();
        }

        private T[] GetListAll<T>(Func<T, bool> predicate) where T : class, new()
        {
            List<T> list = Task.Run(() => 
                _manager.ListAll<T>(
                    new NameValueCollection()
                    {
                        { RedmineKeys.ASSIGNED_TO_ID, Options.Sync.UserId.ToString() },
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
            {
                _issues[issue.Id] = issue;
                Notify?.Invoke(this, new TaskCommon
                {
                    Id = issue.Id.ToString(),
                    Context = new TaskContext { Name = issue.Subject, Desc = issue.Description, Status = issue.Status.Name }
                });
            }

            return true;
        }

        private bool SyncStatuses()
        {
            IssueStatusRM[] updates = GetListAll<IssueStatusRM>(status => !_statuses.ContainsKey(status.Id) || !_statuses[status.Id].Equals(status));

            for (int i = 0; i < updates.Length; i++)
            {
                _statuses[updates[i].Id] = new QueueItem<IssueStatusRM>(updates[i], i < updates.Length - 1 ? updates[i + 1] : null);
            }

            return true;
        }

        private bool SyncProjects()
        {
            ProjectRM[] updates = GetListAll<ProjectRM>(project => !_projects.ContainsKey(project.Id) || !_projects[project.Id].Equals(project));

            foreach (ProjectRM item in updates)
                _projects[item.Id] = item;

            return true;
        }

        #endregion Methods
    }
}
