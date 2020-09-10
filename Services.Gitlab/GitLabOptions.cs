namespace Services.GitLab
{
    public class GitLabOptions : IGitLabOptions, IGitLabSync
    {
        public string Host { get; set; }

        public string Token { get; set; }

        IGitLabSync IGitLabOptions.Sync => this;

        public int Interval { get; set; }

        public int UserId { get; set; }

        public int AssignedId { get; set; }

        public int ProjectId { get; set; }

        public string SearchBranches { get; set; }
    }
}
