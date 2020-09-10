namespace Services.GitLab.Tasks
{
    using System;

    using Common.Tasks;

    public class UpdateMergeRequestTask : TaskItem<IGitLabService, int>, IUpdateMergeRequestTask
    {
        public int? Id { get; }

        public int ProjectId { get; }

        public string SourceBranch { get; }

        public string TargetBranch { get; }

        public UpdateMergeRequestTask(int projectId, string sourceBranch, string targetBranch, int? id = null, Action<int> callback = null) : base(callback)
        {
            Id = id;
            ProjectId = projectId;
            SourceBranch = sourceBranch;
            TargetBranch = targetBranch;
        }

        protected override int HandleImpl(IGitLabService service)
        {
            return service.Handle(this);
        }
    }
}
