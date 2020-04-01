namespace Redmine2Trello.Services
{
    using CommandLine;

    class RedmineOptions : IRedmineOptions, IRedmineSync
    {
        [Option("rm_host", Required = true, ResourceType = typeof(string))]
        public string Host { get; set; }

        [Option("rm_apikey", Required = true, ResourceType = typeof(string))]
        public string ApiKey { get; set; }

        IRedmineSync IRedmineOptions.Sync => this;

        [Option("rm_interval", Required = true, ResourceType = typeof(int))]
        public int Interval { get; set; }

        [Option("rm_assigned", Required = true, ResourceType = typeof(int))]
        public int AssignedId { get; set; }

        [Option("rm_statuses", Required = true, ResourceType = typeof(int[]))]
        public int[] StatusIds { get; set; }
    }
}
