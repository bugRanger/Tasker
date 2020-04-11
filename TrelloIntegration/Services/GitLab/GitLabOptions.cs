namespace TrelloIntegration.Services
{
    using CommandLine;

    class GitLabOptions : IGitLabOptions, IGitLabSync
    {
        [Option("gl_host", Required = true, ResourceType = typeof(string))]
        public string Host { get; set; }

        [Option("gl_token", Required = true, ResourceType = typeof(string))]
        public string Token { get; set; }

        IGitLabSync IGitLabOptions.Sync => this;

        [Option("gl_interval", Required = true, ResourceType = typeof(int))]
        public int Interval { get; set; }

        public int UserId { get; set; }
    }
}
