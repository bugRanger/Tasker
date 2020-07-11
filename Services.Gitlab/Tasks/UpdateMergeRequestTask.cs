namespace Services.GitLab.Tasks
{
    using System;

    using Common.Tasks;

    public class UpdateMergeRequestTask : TaskItem<IGitLabService, bool>, IUpdateMergeRequestTask
    {
        public int ProjectId { get; }

        public string SourceBranch { get; }

        public string TargetBranch { get; }

        public string Title { get; }

        public UpdateMergeRequestTask(int projectId, string sourceBranch, string targetBranch, string title, Action<bool> callback = null) : base(callback)
        {
            ProjectId = projectId;
            SourceBranch = sourceBranch;
            TargetBranch = targetBranch;
            Title = title;
        }

        protected override bool HandleImpl(IGitLabService service)
        {
            return service.Handle(this);
        }
    }
}
