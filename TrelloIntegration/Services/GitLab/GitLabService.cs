﻿namespace TrelloIntegration.Services.GitLab
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;

    using TrelloIntegration.Common.Tasks;
    using TrelloIntegration.Services.GitLab.Tasks;

    using GitLabApiClient;
    using GitLabApiClient.Models.MergeRequests.Responses;
    using GitLabApiClient.Models.MergeRequests.Requests;

    class GitLabService : ITaskService, IDisposable
    {
        #region Fields

        private GitLabClient _client;
        private IGitLabOptions _options;
        private Dictionary<int, MergeRequest> _requests;
        private ITaskQueue<GitLabService> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<MergeRequest[]> UpdateRequests;
        public event EventHandler<string> Error;

        #endregion Events

        #region Constructors

        public GitLabService(IGitLabOptions options)
        {
            _requests = new Dictionary<int, MergeRequest>();
            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<GitLabService>(task => task.Handle(this));
            _queue.Error += (sender, error) => Error?.Invoke(this, error);
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

            Enqueue(new SyncMergeRequestTask(_options.Sync));
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
            MergeRequest request = Task.Run(() => 
                _client.MergeRequests.CreateAsync(task.ProjectId, new CreateMergeRequest(task.SourceBranch, task.TargetBranch, task.Title)), 
                _cancellationSource.Token).Result;

            return true;
        }

        public bool Handle(SyncMergeRequestTask task)
        {
            try
            {
                IList<MergeRequest> requests = Task.Run(() => _client.MergeRequests.GetAsync(opt =>
                {
                    opt.AuthorId = task.SyncOptions.UserId;
                }),
                _cancellationSource.Token).Result;

                MergeRequest[] updates = requests
                    .Where(w => !_requests.ContainsKey(w.Id) || !_requests[w.Id].Status.Equals(w.Status))
                    .ToArray();

                foreach (MergeRequest request in updates)
                    _requests[request.Id] = request;

                if (updates.Any())
                    UpdateRequests?.Invoke(this, updates);

                // TODO Handle event.
                return true;
            }
            finally
            {
                if (_queue.HasEnabled())
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(_options.Sync.Interval);
                        Enqueue(new SyncMergeRequestTask(_options.Sync));
                    });
            }
        }

        #endregion Methods
    }
}
