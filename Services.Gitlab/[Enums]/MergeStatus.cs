namespace Services.GitLab
{
    using System;

    using GitLabApiClient.Models.MergeRequests.Responses;

    public enum MergeStatus
    {
        Opened = MergeRequestState.Opened,
        Active = MergeRequestState.Active,
        Merged = MergeRequestState.Merged,
        Closed = MergeRequestState.Closed,
        Reopened = MergeRequestState.Reopened,
    }
}
