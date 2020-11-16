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
            string branchName = GetBranchName(task.Context);

            switch (task.Context.Status)
            {
                case TaskState.OnReview:
                    AddOrUpdateMergeRequest(branchName);
                    break;

                default:
                    break;
            }

            return branchName;
        }

        public void WaitSync() => _queue.IsEmpty();

        private bool SyncMergeRequests(Action<MergeRequestsQueryOptions> expression)
        {
            if (expression == null)
                return false;

            try
            {
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

            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }

            return true;
        }

        private string GetBranchName(ITaskContext task)
        {
            var folder =
                task.Kind == TaskKind.Defect ? "bugfix" :
                task.Kind == TaskKind.Task ? "feature" :
                throw new ArgumentException(nameof(task.Kind));

            return $"{folder}/{task.Id}";
        }

        private void AddOrUpdateMergeRequest(string branchName)
        {
            if (!_branches.TryGetValue(branchName, out Branch branch))
            {
                branch = RunAsync(() => _proxy.GetAsync(Options.ProjectId, branchName));
                if (branch == null)
                {
                    return;
                }
            }

            if (!_requests.TryGetValue(branchName, out MergeRequest request))
            {
                request =
                    RunAsync(() =>
                    {
                        return _proxy.CreateAsync(Options.ProjectId,
                            new CreateMergeRequest(branch.Name, Options.TargetBranch, branch.Commit.Title)
                            {
                                AssigneeId = Options.AssignedId,
                                RemoveSourceBranch = true,
                            });
                    });
            }
            else if (request.Title != branch.Commit.Title)
            {
                request =
                    RunAsync(() =>
                    {
                        return _proxy.UpdateAsync(Options.ProjectId, request.Id,
                            new UpdateMergeRequest
                            {
                                AssigneeId = Options.AssignedId,
                                RemoveSourceBranch = true,

                                TargetBranch = Options.TargetBranch,
                                Title = branch.Commit.Title,
                            });
                    });
            }

            if (request != null)
            {
                _requests[branch.Name] = request;
            }
        }

        private T RunAsync<T>(Func<Task<T>> action)
        {
            var result = default(T);

            try
            {
                result = Task.Run(action, _cancellationSource.Token).Result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }

            return result;
        }

        #endregion Methods
    }
}
