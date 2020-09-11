namespace Services.GitLab
{
    using System;

    public interface IGitLabStrategy
    {
        #region Methods

        void Register(IGitLabService visitor);

        void UpdateMerges(MergeRequest[] mergeRequests);

        void UpdateBranches(Branch[] branches);

        #endregion Methods

    }
}
