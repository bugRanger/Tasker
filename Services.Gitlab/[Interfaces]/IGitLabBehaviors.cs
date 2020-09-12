namespace Services.GitLab
{
    using System;

    public interface IGitLabBehaviors
    {
        #region Methods

        void UpdateMerges(MergeRequest[] mergeRequests);

        void UpdateBranches(Branch[] branches);

        #endregion Methods
    }
}
