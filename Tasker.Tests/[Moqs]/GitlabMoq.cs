namespace Tasker.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using Moq;

    using Framework.Tests;

    using Services.Gitlab;

    using GitLabApiClient.Internal.Paths;
    using GitLabApiClient.Models;
    using GitLabApiClient.Models.Branches.Responses;
    using GitLabApiClient.Models.Branches.Requests;
    using GitLabApiClient.Models.MergeRequests.Responses;
    using GitLabApiClient.Models.MergeRequests.Requests;

    internal class GitlabMoq
    {
        #region Classes

        internal class CreateBranch : MethodCallEntry
        {
            internal CreateBranch(string branchName, string projectId) : base(branchName, projectId) { }
        }

        internal class CreateMergeRequest : MethodCallEntry
        {
            internal CreateMergeRequest(MergeRequest mergeRequest) 
                : this(
                      mergeRequest.Id, 
                      mergeRequest.ProjectId.ToString(), 
                      mergeRequest.Title, 
                      mergeRequest.SourceBranch, 
                      mergeRequest.TargetBranch, 
                      mergeRequest.Assignee.Id, 
                      mergeRequest.ForceRemoveSourceBranch) { }

            internal CreateMergeRequest(int id, string projectId, string title, string sourceBranch, string targetBranch, int assignee, bool remove) 
                : base(id, projectId, title, sourceBranch, targetBranch, assignee, remove) { }
        }

        #endregion Classes

        #region Properties

        public Mock<IGitlabProxy> Proxy { get; }

        public Dictionary<string, Branch> Branches { get; }

        public Dictionary<string, MergeRequest> MergeRequests { get; }

        #endregion Properties

        #region Constructors

        public GitlabMoq(Action<MethodCallEntry> handleEvent) 
        {
            Branches = new Dictionary<string, Branch>();
            MergeRequests = new Dictionary<string, MergeRequest>();

            Proxy = new Mock<IGitlabProxy>();

            Proxy.Setup(s => s.GetAsync(It.IsAny<ProjectId>(), It.IsAny<Action<BranchQueryOptions>>())).Returns<ProjectId, Action<BranchQueryOptions>>((id, opt) => 
            {
                IList<Branch> result = Branches.Values.ToList();
                return Task.FromResult(result);
            });

            Proxy.Setup(s => s.CreateAsync(It.IsAny<ProjectId>(), It.IsAny<CreateBranchRequest>())).Returns<ProjectId, CreateBranchRequest>((id, opt) =>
            {
                var result = new Branch
                {
                    Name = opt.Branch,
                };
                Branches[result.Name] = result;

                handleEvent?.Invoke(new CreateBranch(result.Name, id.ToString()));

                return Task.FromResult(result);
            });

            Proxy.Setup(s => s.GetAsync(It.IsAny<ProjectId>(), It.IsAny<Action<MergeRequestsQueryOptions>>())).Returns<ProjectId, Action<MergeRequestsQueryOptions>>((id, opt) =>
            {
                IList<MergeRequest> result = MergeRequests.Values.ToList();
                return Task.FromResult(result);
            });

            Proxy.Setup(s => s.CreateAsync(It.IsAny<ProjectId>(), It.IsAny<GitLabApiClient.Models.MergeRequests.Requests.CreateMergeRequest>())).Returns<ProjectId, GitLabApiClient.Models.MergeRequests.Requests.CreateMergeRequest>((id, opt) =>
            {
                var result = new MergeRequest
                {
                    Id = MergeRequests.Count + 1,
                    ProjectId = id.ToString(),
                    Title = opt.Title,
                    SourceBranch = opt.SourceBranch,
                    TargetBranch = opt.TargetBranch,
                    Assignee = opt.AssigneeId.HasValue ? new Assignee { Id = opt.AssigneeId.Value } : null,
                    ForceRemoveSourceBranch = opt.RemoveSourceBranch ?? false,
                };
                MergeRequests[result.SourceBranch] = result;

                handleEvent?.Invoke(new CreateMergeRequest(result));

                return Task.FromResult(result);
            });
        }

        #endregion Constructors
    }
}
