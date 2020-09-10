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
    using GitLabApiClient.Models.Branches.Requests;

    public class GitLabService : IGitLabService, IDisposable
    {
        #region Fields

        private ILogger _logger;
        private GitLabClient _client;
        private IGitLabOptions _options;
        private Dictionary<int, MergeRequest> _requests;
        private Dictionary<string, Branch> _branches;
        private ITaskQueue<IGitLabService> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<MergeRequestEventArgs[]> MergeRequestsNotify;
        public event EventHandler<BranchesEventArgs[]> UpdateBranches;

        #endregion Events

        #region Constructors

        public GitLabService(IGitLabOptions options, ITimelineEnviroment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _requests = new Dictionary<int, MergeRequest>();
            _branches = new Dictionary<string, Branch>();
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

            //Enqueue(new SyncActionTask<IGitLabService>(() => SyncMergeRequests(opt => opt.AssigneeId = _options.Sync.UserId), _queue, _options.Sync.Interval));
            Enqueue(new SyncActionTask<IGitLabService>(() => SyncMergeRequests(opt => opt.AuthorId = _options.Sync.UserId), _queue, _options.Sync.Interval));
            Enqueue(new SyncActionTask<IGitLabService>(() => SyncBranches(opt => opt.Search = _options.Sync.SearchBranches), _queue, _options.Sync.Interval));
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

        public int Handle(IUpdateMergeRequestTask task)
        {
            MergeRequest request = null;

            if (!_branches.TryGetValue(task.SourceBranch, out var branch))
            {
                _logger?.Debug(() => $"Not found source branch: `{task.SourceBranch}`");
                return 0;
            }

            if (!task.Id.HasValue || !_requests.TryGetValue(task.Id.Value, out var _))
            {

                request = Task.Run(() =>
                    _client.MergeRequests.CreateAsync(task.ProjectId, new CreateMergeRequest(task.SourceBranch, task.TargetBranch, branch.Commit.Message)),
                    _cancellationSource.Token).Result;
            }
            else
            {
                request = Task.Run(() =>
                    _client.MergeRequests.UpdateAsync(task.ProjectId, task.Id.Value, new UpdateMergeRequest()
                    {
                        Title = branch.Commit.Message,
                        AssigneeId = _options.AssignedId,
                        TargetBranch = task.TargetBranch,
                        RemoveSourceBranch = true,
                    }),
                    _cancellationSource.Token).Result;
            }

            _requests[request.Id] = request;
            return request.Id;
        }

        private bool SyncBranches(Action<BranchQueryOptions> expression)
        {
            if (expression == null)
                return false;

            IList<Branch> branches = Task.Run(() => _client.Branches.GetAsync(_options.ProjectId, expression), _cancellationSource.Token).Result;
            List<Branch> updates = branches
                .Where(w => !_branches.TryGetValue(w.Name, out var req))
                .ToList();

            if (!updates.Any())
                return true;

            Dictionary<string, BranchesEventArgs> notifications = 
                updates
                .Select(s => new BranchesEventArgs(s.Name, s.Commit.Title))
                .ToDictionary(k => k.Name);

            UpdateBranches?.Invoke(this, notifications.Values.ToArray());

            updates.ForEach(item =>
            {
                if (notifications.TryGetValue(item.Name, out var branch))
                    _branches[item.Name] = item;
            });

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
                    || req.WorkInProgress != w.WorkInProgress
                    || req.MergeWhenPipelineSucceeds != w.MergeWhenPipelineSucceeds)
                .ToList();

            if (!updates.Any())
                return true;

            Dictionary<int, MergeRequestEventArgs> notifications = 
                updates
                .Select(request => new MergeRequestEventArgs()
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
