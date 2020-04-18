namespace TrelloIntegration.Services.GitLab.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateMergeRequestTask : TaskItem<GitLabService, bool>
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
