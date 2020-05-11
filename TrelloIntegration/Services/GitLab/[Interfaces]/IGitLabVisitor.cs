namespace TrelloIntegration.Services.GitLab
{
    using System;

    using TrelloIntegration.Common.Tasks;
    using TrelloIntegration.Services.GitLab.Tasks;

    interface IGitLabVisitor : IServiceVisitor
    {
        #region Methods

        bool Handle(UpdateMergeRequestTask task);

        #endregion Methods
    }
}
