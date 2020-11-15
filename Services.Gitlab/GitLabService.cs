namespace Services.Gitlab
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

    using GitLabApiClient.Models.MergeRequests.Requests;
    using GitLabApiClient.Models.MergeRequests.Responses;
    using GitLabApiClient.Models.Branches.Requests;
    using GitLabApiClient.Models.Branches.Responses;

    public class GitLabService : ITaskService, ITaskVisitor, IDisposable
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly ITimelineEnvironment _timeline;

        private readonly CancellationTokenSource _cancellationSource;

        private readonly TaskQueue _queue;

        private readonly Dictionary<string, MergeRequest> _requests;
        private readonly Dictionary<string, Branch> _branches;

        private IGitlabProxy _proxy;

        #endregion Fields

        #region Properties

        public IGitLabOptions Options { get; }

        #endregion Properties

        #region Events

        public event Action<object, ITaskCommon, IEnumerable<string>> Notify;

        #endregion Events

        #region Constructors

        public GitLabService(IGitLabOptions options, ITimelineEnvironment timeline, IGitlabProxy proxy = null) 
            : this(options, timeline)
        {
            _proxy = proxy;
        }

        public GitLabService(IGitLabOptions options, ITimelineEnvironment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            Options = options;

            _timeline = timeline;
            _branches = new Dictionary<string, Branch>();
            _requests = new Dictionary<string, MergeRequest>();
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

            _proxy ??= new GitlabProxy(Options.Host, Options.Token);
            _queue.Start();

            Enqueue(new ActionTask(() => SyncBranches(opt => opt.Search = Options.Sync.SearchBranches), Options.Sync.Interval) { LastTime = _timeline.TickCount });
            Enqueue(new ActionTask(() => SyncMergeRequests(opt => opt.AuthorId = Options.Sync.UserId), Options.Sync.Interval) { LastTime = _timeline.TickCount });
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

        public string Handle(ITaskCommon task)
        {
            string branchId = task.ExternalId ?? $"{task.Context.Kind}/{task.Context.Id}";
            string branchRef = "origin/release/tmp-9.9";

            switch (task.Context.Status)
            {
                case TaskState.InProgress:
                    if (string.IsNullOrWhiteSpace(branchId) || !_branches.TryGetValue(branchId, out _))
                    {
                        _branches[branchId] =
                            RunAsync(() =>
                            {
                                return _proxy.CreateAsync(Options.ProjectId, new CreateBranchRequest(branchId, branchRef));
                            });
                    }

                    break;

                case TaskState.OnReview:
                    if (string.IsNullOrWhiteSpace(branchId) || !_branches.TryGetValue(branchId, out Branch branch))
                    {
                        break;
                    }

                    if (_requests.TryGetValue(branchId, out MergeRequest request))
                    {
                        if (request.Title != branch.Commit.Title)
                        {
                            _requests[branchId] =
                                RunAsync(() =>
                                {
                                    return _proxy.UpdateAsync(Options.ProjectId, request.Id,
                                        new UpdateMergeRequest()
                                        {
                                            AssigneeId = Options.AssignedId,
                                            RemoveSourceBranch = true,
                                            Title = branch.Commit.Title,
                                            TargetBranch = branchRef,
                                        });
                                });
                        }
                        break;
                    }

                    _requests[branchId] =
                        RunAsync(() =>
                        {
                            return _proxy.CreateAsync(Options.ProjectId,
                                new CreateMergeRequest(branchId, branchRef, branch.Commit.Title)
                                {
                                    AssigneeId = Options.AssignedId,
                                    RemoveSourceBranch = true,
                                });
                        });

                    break;
            }

            return branchId;
        }

        public void WaitSync() => _queue.IsEmpty();

        private bool SyncBranches(Action<BranchQueryOptions> expression)
        {
            if (expression == null)
            {
                return false;
            }

            IList<Branch> updates = RunAsync(() => _proxy.GetAsync(Options.ProjectId, expression));

            if (updates.Count == 0)
            {
                return false;
            }

            foreach (var item in updates)
            {
                // TODO Impl equeals.
                if (_branches.TryGetValue(item.Name, out var branch) && item.Equals(branch))
                    continue;

                _branches[item.Name] = item;
            }

            return true;
        }

        private bool SyncMergeRequests(Action<MergeRequestsQueryOptions> expression)
        {
            if (expression == null)
                return false;

            IList<MergeRequest> updates = RunAsync(() => _proxy.GetAsync(Options.ProjectId, expression));

            if (updates.Count == 0)
            {
                return false;
            }

            foreach (var item in updates)
            {
                if (_requests.TryGetValue(item.SourceBranch, out var req) && item.State != req.State)
                {
                    continue;
                }

                _requests[item.SourceBranch] = item;

                if (item.State != MergeRequestState.Merged)
                {
                    continue;
                }

                Notify?.Invoke(this,
                    new TaskCommon
                    {
                        ExternalId = item.SourceBranch,
                        Context = new TaskContext { Status = TaskState.Resolved }
                    },
                    new string[]
                    {
                        nameof(TaskContext.Status),
                    });
            }

            return true;
        }

        private T RunAsync<T>(Func<Task<T>> action)
        {
            return Task.Run(action, _cancellationSource.Token).Result;
        }

        #endregion Methods
    }
}
