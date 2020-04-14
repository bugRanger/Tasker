namespace TrelloIntegration.Services.GitLab
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;

    using TrelloIntegration.Common;
    using TrelloIntegration.Services.GitLab.Tasks;

    using GitLabApiClient;

    class GitLabService : ITaskService, IDisposable
    {
        #region Fields

        private GitLabClient _client;
        private IGitLabOptions _options;
        private ITaskQueue<GitLabService> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<object> UpdateMergeRequest;

        #endregion Events

        #region Constructors

        public GitLabService(IGitLabOptions options)
        {
            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<GitLabService>(task => task.Handle(this));
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

            _client = _client ?? new GitLabClient(_options.Host, _options.Token);
            _queue.Start();

            //Enqueue(new SyncMergeRequestTask(_options.Sync));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem<GitLabService> task)
        {
            _queue.Enqueue(task);
        }

        public bool Handle(UpdateMergeRequestTask task)
        {
            //Task.Run(() => _client.MergeRequests.
            // TODO Update info.
            return true;
        }

        public bool Handle(SyncMergeRequestTask task)
        {
            var requests = Task.Run(() => _client.MergeRequests.GetAsync(opt =>
            {
                opt.AuthorId = task.SyncOptions.UserId;
            }),
            _cancellationSource.Token).Result;
            
            // TODO Handle event.

            if (_queue.HasEnabled())
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_options.Sync.Interval);
                    Enqueue(new SyncMergeRequestTask(_options.Sync));
                });

            return true;
        }

        #endregion Methods
    }
}
