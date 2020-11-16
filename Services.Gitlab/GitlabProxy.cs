namespace Services.Gitlab
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using GitLabApiClient;
    using GitLabApiClient.Internal.Paths;
    using GitLabApiClient.Models.Branches.Requests;
    using GitLabApiClient.Models.Branches.Responses;
    using GitLabApiClient.Models.MergeRequests.Responses;
    using GitLabApiClient.Models.MergeRequests.Requests;

    public class GitlabProxy : IGitlabProxy
    {
        #region Fields

        private readonly GitLabClient _client;

        #endregion Fields

        #region Constructors

        public GitlabProxy(string hostUrl, string authenticationToken)
        {
            _client = new GitLabClient(hostUrl, authenticationToken);
        }

        #endregion Constructors

        #region Methods

        public Task<Branch> CreateAsync(ProjectId projectId, CreateBranchRequest request)
        {
            return _client.Branches.CreateAsync(projectId, request);
        }

        public Task<Branch> GetAsync(ProjectId projectId, string branchName)
        {
            return _client.Branches.GetAsync(projectId, branchName);
        }


        public Task<MergeRequest> CreateAsync(ProjectId projectId, CreateMergeRequest request)
        {
            return _client.MergeRequests.CreateAsync(projectId, request);
        }

        public Task<IList<MergeRequest>> GetAsync(ProjectId projectId, Action<ProjectMergeRequestsQueryOptions> options = null) 
        {
            return _client.MergeRequests.GetAsync(projectId, options);
        }

        public Task<MergeRequest> UpdateAsync(ProjectId projectId, int mergeRequestId, UpdateMergeRequest request)
        {
            return _client.MergeRequests.UpdateAsync(projectId, mergeRequestId, request);
        }

        #endregion Methods
    }
}