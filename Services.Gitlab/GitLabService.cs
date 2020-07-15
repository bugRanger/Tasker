﻿namespace Services.GitLab
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using Common;
    using Common.Tasks;

    using Services.GitLab.Tasks;

    using GitLabApiClient;
    using GitLabApiClient.Models.MergeRequests.Requests;
    using GitLabApiClient.Models.MergeRequests.Responses;
    using GitLabApiClient.Models.Branches.Responses;

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

        public event EventHandler<MergeRequest[]> UpdateRequests;
        public event EventHandler<Branch[]> UpdateBranches;
        public event EventHandler<string> Error;

        #endregion Events

        #region Constructors

        public GitLabService(IGitLabOptions options, ITimelineEnviroment timeline)
        {
            _requests = new Dictionary<int, MergeRequest>();
            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<IGitLabService>(task => task.Handle(this), timeline);
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

            Enqueue(new SyncActionTask<IGitLabService>(SyncMergeRequests, _queue, _options.Sync.Interval));
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

        private bool SyncMergeRequests() 
        {
            IList<MergeRequest> requests = Task.Run(() => _client.MergeRequests.GetAsync(opt =>
            {
                opt.AuthorId = _options.Sync.UserId;
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

        #endregion Methods
    }
}