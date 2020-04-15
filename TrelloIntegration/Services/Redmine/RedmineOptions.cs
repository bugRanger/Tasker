namespace TrelloIntegration.Services
{
    class RedmineOptions : IRedmineOptions, IRedmineSync
    {
        public string Host { get; set; }

        public string ApiKey { get; set; }

        IRedmineSync IRedmineOptions.Sync => this;

        public int Interval { get; set; }

        public float EstimatedHoursABS { get; set; }

        public int UserId { get; set; }

        public int[] Statuses { get; set; }
    }
}
