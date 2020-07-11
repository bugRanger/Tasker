namespace Services.GitLab.Tasks
{
    using Common.Tasks;

    public interface IUpdateMergeRequestTask : ITaskItem<IGitLabService>
    {
        #region Properties

        int ProjectId { get; }

        string SourceBranch { get; }

        string TargetBranch { get; }

        string Title { get; }

        #endregion Properties
    }
}