namespace Services.GitLab
{
    using Common.Tasks;
    using Services.GitLab.Tasks;

    public interface IGitLabService : ITaskVisitor
    {
        #region Methods

        int Handle(IUpdateMergeRequestTask task);

        #endregion Methods
    }
}
