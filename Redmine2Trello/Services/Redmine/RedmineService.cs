namespace Redmine2Trello.Services
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Specialized;
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
        private TaskQueue<TaskItem<RedmineService>> _queue;
        private Dictionary<int, Issue> _issues;
        private Dictionary<int, IssueStatus> _statuses;

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

            _options = options;
            _manager = new RedmineManager(options.Host, options.ApiKey);
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

            Enqueue(new SyncIssuesTask(_options.Sync));
        }

        public override void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _queue.Stop();
        }

        public void Enqueue(TaskItem<RedmineService> task)
        {
            _queue.Enqueue(task);
        }

        public async void Handle(UpdateIssueTask task)
        {
            Issue issue = await _manager.Get<Issue>(task.IssueId.ToString(), null);
            IssueStatus status = await _manager.Get<IssueStatus>(task.StatusId.ToString(), null);

            issue.Status = status;

            issue = await _manager.Update(task.IssueId.ToString(), issue);
            if (!_issues.ContainsKey(issue.Id))
                _issues[issue.Id] = issue;
        }

        public async void Handle(SyncStatusesTask task)
        {
            List<IssueStatus> statuses = await _manager.ListAll<IssueStatus>(new NameValueCollection());

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
        }

        public async void Handle(SyncIssuesTask task)
        {
            List<Issue> issues = await _manager.ListAll<Issue>(
                new NameValueCollection()
                {
                    { RedmineKeys.ASSIGNED_TO_ID, task.SyncOptions.AssignedId.ToString() },
                });

            Issue[] updates = issues
                .Where(w => !_issues.ContainsKey(w.Id) || !_issues[w.Id].Equals(w))
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
        }

        #endregion Methods
    }
}
