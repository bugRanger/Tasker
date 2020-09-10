namespace Services.GitLab
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using NLog;

    using Common.Tasks;
    using Framework.Common;

    using GitLabApiClient;
    using GitLabApiClient.Models.MergeRequests.Requests;
    using GitLabApiClient.Models.MergeRequests.Responses;
    using GitLabApiClient.Models.Branches.Responses;

    using Tasks;

    public class GitLabService : IGitLabService, IDisposable
    {
        #region Fields

        private GitLabClient _client;
        private IGitLabOptions _options;
        private Dictionary<int, MergeRequest> _requests;
        private ITaskQueue<IGitLabService> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        private ILogger _logger;
        public event EventHandler<MergeRequestNotifyEvent[]> MergeRequestsNotify;
        public event EventHandler<Branch[]> UpdateBranches;

        #endregion Events

        #region Constructors

        public GitLabService(IGitLabOptions options, ITimelineEnviroment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _requests = new Dictionary<int, MergeRequest>();
            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<IGitLabService>(task => task.Handle(this), timeline);
            _queue.Error += (task, error) => _logger?.Error($"failed task: {task}, error: `{error}`"); ;
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            if (_queue.HasEnabled())
                return;

            _client ??= new GitLabClient(_options.Host, _options.Token);
            _queue.Start();

            Enqueue(new SyncActionTask<IGitLabService>(() => SyncMergeRequests(opt => opt.AssigneeId = _options.Sync.UserId), _queue, _options.Sync.Interval));
            Enqueue(new SyncActionTask<IGitLabService>(() => SyncMergeRequests(opt => opt.AuthorId = _options.Sync.UserId), _queue, _options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem<IGitLabService> task)
        {
            _queue.Enqueue(task);
        }

        public bool Handle(IUpdateMergeRequestTask task)
        {
            MergeRequest request = Task.Run(() => 
                _client.MergeRequests.CreateAsync(task.ProjectId, new CreateMergeRequest(task.SourceBranch, task.TargetBranch, task.Title)), 
                _cancellationSource.Token).Result;

            return true;
        }

        private bool SyncMergeRequests(Action<MergeRequestsQueryOptions> expression) 
        {
            if (expression == null)
                return false;

            IList<MergeRequest> mergeRequests = Task.Run(() => _client.MergeRequests.GetAsync(expression), _cancellationSource.Token).Result;
            List<MergeRequest> updates = mergeRequests
                .Where(w => 
                    !_requests.TryGetValue(w.Id, out var req) 
                    || req.State != w.State 
                    || req.WorkInProgress != w.WorkInProgress)
                .ToList();

            if (!updates.Any())
                return true;

            Dictionary<int, MergeRequestNotifyEvent> notifications = 
                updates.Select(request => new MergeRequestNotifyEvent()
                {
                    Id = request.Id,
                    Title = request.Title,
                    State = request.State,
                    Url = request.WebUrl,
                    WorkInProgress = request.WorkInProgress,
                    Handle = false,
                })
                .ToDictionary(k => k.Id);

            MergeRequestsNotify?.Invoke(this, notifications.Values.ToArray());

            updates.ForEach(item =>
            {
                if (notifications.TryGetValue(item.Id, out var mergeNotify) && mergeNotify.Handle)
                    _requests[item.Id] = item;
            });

            return true;
        }

        #endregion Methods
    }
}
