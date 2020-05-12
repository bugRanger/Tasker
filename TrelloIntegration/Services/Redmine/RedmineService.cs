namespace TrelloIntegration.Services.Redmine
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;

    using TrelloIntegration.Common.Tasks;
    using TrelloIntegration.Services.Redmine.Tasks;

    using RedmineApi.Core;
    using RedmineApi.Core.Types;

    class RedmineService : IRedmineVisitor, ITaskService, IDisposable
    {
        #region Fields

        private IRedmineOptions _options;
        private RedmineManager _manager;
        private Dictionary<int, Project> _projects;
        private Dictionary<int, Issue> _issues;
        private Dictionary<int, IssueStatus> _statuses;
        private ITaskQueue<RedmineService> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<Project[]> UpdateProjects;
        public event EventHandler<Issue[]> UpdateIssues;
        public event EventHandler<IssueStatus[]> UpdateStatuses;
        public event EventHandler<string> Error;

        #endregion Events

        #region Constructors

        public RedmineService(IRedmineOptions options)
        {
            _projects = new Dictionary<int, Project>();
            _issues = new Dictionary<int, Issue>();
            _statuses = new Dictionary<int, IssueStatus>();

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<RedmineService>(task => task.Handle(this));
            _queue.Error += (sender, error) => Error?.Invoke(this, error);
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

            Enqueue(new SyncActionTask<RedmineService>(SyncProjects));
            Enqueue(new SyncActionTask<RedmineService>(SyncStatuses));
            Enqueue(new SyncActionTask<RedmineService>(SyncIssues, _queue, _options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem<RedmineService> task)
        {
            _queue.Enqueue(task);
        }

        public bool Handle(UpdateWorkTimeTask task)
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

        public bool Handle(UpdateIssueTask task)
        {
            Issue issue = Task.Run(() => _manager.Get<Issue>(task.IssueId.ToString(), null), _cancellationSource.Token).Result;

            // TODO Add script for redmine actions on change status.
            issue.Status = new IssueStatus() { Id = task.StatusId };

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

            foreach (IssueStatus item in updates)
                _statuses[item.Id] = item;

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
