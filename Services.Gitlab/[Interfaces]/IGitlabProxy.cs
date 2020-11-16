namespace Services.Gitlab
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using GitLabApiClient.Internal.Paths;
    using GitLabApiClient.Models.Branches.Requests;
    using GitLabApiClient.Models.Branches.Responses;
    using GitLabApiClient.Models.MergeRequests.Requests;
    using GitLabApiClient.Models.MergeRequests.Responses;

    public interface IGitlabProxy
    {
        #region Methods

        Task<Branch> CreateAsync(ProjectId projectId, CreateBranchRequest request);

        Task<MergeRequest> CreateAsync(ProjectId projectId, CreateMergeRequest request);

        Task<Branch> GetAsync(ProjectId projectId, string branchName);

        Task<IList<MergeRequest>> GetAsync(ProjectId projectId, Action<ProjectMergeRequestsQueryOptions> options = null);

        Task<MergeRequest> UpdateAsync(ProjectId projectId, int mergeRequestId, UpdateMergeRequest request);

        #endregion Methods
    }
}
