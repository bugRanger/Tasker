namespace Services.GitLab
{
    using Common.Tasks;
    using Services.GitLab.Tasks;

    public interface IGitLabService : ITaskVisitor
    {
        #region Methods

        bool Handle(IUpdateMergeRequestTask task);

        #endregion Methods
    }
}
