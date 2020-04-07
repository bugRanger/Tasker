namespace Redmine2Trello.Services
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;

    using Redmine2Trello.Common;
    using Redmine2Trello.Services.Redmine.Tasks;

    using RedmineApi.Core;
    using RedmineApi.Core.Types;

    class RedmineService : TaskService, IDisposable
    {
        #region Fields

        private IRedmineOptions _options;
        private RedmineManager _manager;
        private Dictionary<int, Issue> _issues;
        private Dictionary<int, IssueStatus> _statuses;
        private TaskQueue<TaskItem<RedmineService>> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<Issue[]> UpdateIssues;
        public event EventHandler<IssueStatus[]> UpdateStatuses;

        #endregion Events

        #region Constructors

        public RedmineService(IRedmineOptions options)
        {
            _issues = new Dictionary<int, Issue>();
            _statuses = new Dictionary<int, IssueStatus>();

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _manager = new RedmineManager(options.Host, options.ApiKey, MimeType.Xml, DefaultRedmineHttpSettings.Create());
            _queue = new TaskQueue<TaskItem<RedmineService>>(task => task.Handle(this));
        }

        public void Dispose()
        {
            Stop();
            _manager?.Dispose();
        }

        #endregion Constructors

        #region Methods

        public override void Start()
        {
            if (_queue.HasEnabled())
                return;

            _queue.Start();

            Enqueue(new SyncStatusesTask());
            Enqueue(new SyncIssuesTask(_options.Sync));
        }

        public override void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(TaskItem<RedmineService> task)
        {
            _queue.Enqueue(task);
        }

        public bool Handle(AddTimeIssueTask task)
        {
            if(!_issues.ContainsKey(task.IssueId))
                return false;
         
            // TODO Add time for issue.   
            Task.Run(() => _manager.Create(new TimeEntry()
            {
                Issue = new IdentifiableName() { Id = _issues[task.IssueId].Id },
                Project = _issues[task.IssueId].Project,
                Hours = task.Hours,
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
                issue.EstimatedHours =  _options.EstimatedHoursABS;

            issue = Task.Run(() => _manager.Update(task.IssueId.ToString(), issue), _cancellationSource.Token).Result;
            if (!_issues.ContainsKey(issue.Id))
                _issues[issue.Id] = issue;

            return true;
        }

        public bool Handle(SyncStatusesTask task)
        {
            List<IssueStatus> statuses = Task.Run(() => _manager.ListAll<IssueStatus>(new NameValueCollection()), _cancellationSource.Token).Result;

            IssueStatus[] updates = statuses
                .Where(w => !_statuses.ContainsKey(w.Id) || !_statuses[w.Id].Equals(w))
                .ToArray();

            foreach (IssueStatus status in updates)
                _statuses[status.Id] = status;

            if (updates.Any())
                UpdateStatuses?.Invoke(this, updates);

            // TODO Move to callback.
            if (_queue.HasEnabled())
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_options.Sync.Interval);
                    Enqueue(new SyncStatusesTask());
                });

            return true;
        }

        public bool Handle(SyncIssuesTask task)
        {
            List<Issue> issues = Task.Run(() =>
                _manager.ListAll<Issue>(
                    new NameValueCollection()
                    {
                        { RedmineKeys.ASSIGNED_TO_ID, task.SyncOptions.AssignedId.ToString() },
                    }),
                _cancellationSource.Token).Result;

            Issue[] updates = issues
                .Where(w => !_issues.ContainsKey(w.Id) || !_issues[w.Id].Status.Equals(w.Status))
                .ToArray();

            foreach (Issue issue in updates)
                _issues[issue.Id] = issue;

            if (updates.Any())
                UpdateIssues?.Invoke(this, updates);

            // TODO Move to callback.
            if (_queue.HasEnabled())
                _ = Task.Run(async () =>
                {
                    await Task.Delay(task.SyncOptions.Interval);
                    Enqueue(new SyncIssuesTask(task.SyncOptions));
                });

            return true;
        }

        #endregion Methods
    }
}
