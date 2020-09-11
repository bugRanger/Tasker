namespace Services.GitLab
{
    using Common.Tasks;

    using Tasks;

    public interface IGitLabVisitor : ITaskVisitor
    {
        #region Methods

        int Handle(IUpdateMergeRequestTask task);

        #endregion Methods
    }
}
