namespace TrelloIntegration.Services.GitLab.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class SyncMergeRequestTask : TaskItem<GitLabService, bool>
    {
        public IGitLabSync SyncOptions { get; }

        public SyncMergeRequestTask(IGitLabSync syncOptions, Action<bool> callback = null) : base(callback)
        {
            SyncOptions = syncOptions;
        }

        protected override bool HandleImpl(GitLabService service)
        {
            return service.Handle(this);
        }
    }
}
