namespace TrelloIntegration.Services.GitLab.Tasks
{
    using System;

    class UpdateMergeRequestTask : Common.TaskItem<GitLabService, bool>
    {
        public UpdateMergeRequestTask(Action<bool> callback = null) : base(callback)
        {
        }

        protected override bool HandleImpl(GitLabService service)
        {
            return service.Handle(this);
        }
    }
}
