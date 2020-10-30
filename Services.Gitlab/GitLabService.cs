namespace Services.GitLab
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using NLog;

    using Framework.Timeline;

    using Tasker.Common.Task;
    using Tasker.Interfaces.Task;

    using GitLabApiClient;
    using GitLabApiClient.Models.MergeRequests.Requests;
    using GitLabApiClient.Models.MergeRequests.Responses;
    using GitLabApiClient.Models.Branches.Requests;
    using GitLabApiClient.Models.Branches.Responses;

    using ResponseBranch = GitLabApiClient.Models.Branches.Responses.Branch;
    using ResponseMR = GitLabApiClient.Models.MergeRequests.Responses.MergeRequest;

    public class GitLabService : ITaskService, ITaskVisitor, IDisposable
    {
        #region Fields

        private ILogger _logger;
        private GitLabClient _client;
        private IGitLabOptions _options;
        private Dictionary<int, ResponseMR> _requests;
        private Dictionary<string, ResponseBranch> _branches;
        private ITaskQueue _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event Action<object, ITaskCommon> Notify;

        #endregion Events

        #region Properties

        public IGitLabOptions Options => _options;

        #endregion Properties

        #region Constructors

        public GitLabService(IGitLabOptions options, ITimelineEnvironment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _options = options;

            _requests = new Dictionary<int, ResponseMR>();
            _branches = new Dictionary<string, ResponseBranch>();
            _cancellationSource = new CancellationTokenSource();

            _queue = new TaskQueue(task => task.Handle(this), timeline);
            _queue.Error += (task, error) => _logger?.Error($"failed task: {task}, error: `{error}`");
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

            //Enqueue(new SyncActionTask<IGitLabVisitor>(() => SyncMergeRequests(opt => opt.AssigneeId = _options.Sync.UserId), _queue, _options.Sync.Interval));
            Enqueue(new SyncActionTask(() => SyncMergeRequests(opt => opt.AuthorId = _options.Sync.UserId), _queue, _options.Sync.Interval));
            Enqueue(new SyncActionTask(() => SyncBranches(opt => opt.Search = _options.Sync.SearchBranches), _queue, _options.Sync.Interval));
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
            ResponseMR request = null;

            //if (!_branches.TryGetValue(task.SourceBranch, out var branch))
            //{
            //    _logger?.Debug(() => $"Not found source branch: `{task.SourceBranch}`");
            //    return 0;
            //}

            //if (!task.Id.HasValue || !_requests.TryGetValue(task.Id.Value, out var _))
            //{

            //    request = Task.Run(() =>
            //        _client.MergeRequests.CreateAsync(task.ProjectId, new CreateMergeRequest(task.SourceBranch, task.TargetBranch, branch.Commit.Message)),
            //        _cancellationSource.Token).Result;
            //}
            //else
            //{
            //    request = Task.Run(() =>
            //        _client.MergeRequests.UpdateAsync(task.ProjectId, task.Id.Value, new UpdateMergeRequest()
            //        {
            //            Title = branch.Commit.Message,
            //            AssigneeId = _options.AssignedId,
            //            TargetBranch = task.TargetBranch,
            //            RemoveSourceBranch = true,
            //        }),
            //        _cancellationSource.Token).Result;
            //}

            _requests[request.Id] = request;
            return request.Id.ToString();
        }

        private bool SyncBranches(Action<BranchQueryOptions> expression)
        {
            if (expression == null)
                return false;

            IList<GitLabApiClient.Models.Branches.Responses.Branch> branches = Task.Run(() => _client.Branches.GetAsync(_options.ProjectId, expression), _cancellationSource.Token).Result;
            List<GitLabApiClient.Models.Branches.Responses.Branch> updates = branches
                .Where(w => !_branches.TryGetValue(w.Name, out var req))
                .ToList();

            if (!updates.Any())
                return true;

            Dictionary<string, Branch> notifications = 
                updates
                .Select(s => new Branch(s.Name, s.Commit.Title))
                .ToDictionary(k => k.Name);

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

            IList<GitLabApiClient.Models.MergeRequests.Responses.MergeRequest> mergeRequests = Task.Run(() => _client.MergeRequests.GetAsync(expression), _cancellationSource.Token).Result;
            List<GitLabApiClient.Models.MergeRequests.Responses.MergeRequest> updates = mergeRequests
                .Where(w => 
                    !_requests.TryGetValue(w.Id, out var req) 
                    || req.State != w.State 
                    || req.WorkInProgress != w.WorkInProgress
                    || req.MergeWhenPipelineSucceeds != w.MergeWhenPipelineSucceeds)
                .ToList();

            if (!updates.Any())
                return true;

            Dictionary<int, MergeRequest> notifications = 
                updates
                .Select(request => new MergeRequest()
                {
                    Id = request.Id,
                    Title = request.Title,
                    State = (MergeStatus)request.State,
                    Url = request.WebUrl,
                    WorkInProgress = request.WorkInProgress,
                })
                .ToDictionary(k => k.Id);

            updates.ForEach(item =>
            {
                if (notifications.TryGetValue(item.Id, out var _))
                    _requests[item.Id] = item;
            });

            // TODO Impl.
            //Notify?.Invoke(this, )

            return true;
        }

        #endregion Methods
    }
}
