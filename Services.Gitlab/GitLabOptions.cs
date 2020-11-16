namespace Services.Gitlab
{
    public class GitLabOptions : IGitLabOptions, IGitlabSync
    {
        #region Properties

        public string Host { get; set; }

        public string Token { get; set; }

        IGitlabSync IGitLabOptions.Sync => this;

        public int Interval { get; set; }

        public int UserId { get; set; }

        public int AssignedId { get; set; }

        public int ProjectId { get; set; }

        public string TargetBranch { get; set; }

        #endregion Properties
    }
}
