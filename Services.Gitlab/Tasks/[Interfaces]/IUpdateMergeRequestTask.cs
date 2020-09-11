namespace Services.GitLab.Tasks
{
    using Common.Tasks;

    public interface IUpdateMergeRequestTask : ITaskItem<IGitLabVisitor>
    {
        #region Properties

        int? Id { get; }

        int ProjectId { get; }

        string SourceBranch { get; }

        string TargetBranch { get; }

        #endregion Properties
    }
}