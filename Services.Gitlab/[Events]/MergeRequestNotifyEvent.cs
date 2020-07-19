namespace Services.GitLab
{
    using GitLabApiClient.Models.MergeRequests.Responses;

    public class MergeRequestNotifyEvent
    {
        public int Id { get; internal set; }

        public string Url { get; internal set; }

        public string Title { get; internal set; }

        public MergeRequestState State { get; internal set; }

        public bool Handle { get; set; }
    }
}
