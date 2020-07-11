namespace Services.Redmine
{
    public class RedmineOptions : IRedmineOptions, IRedmineSync
    {
        public string Host { get; set; }

        public string ApiKey { get; set; }

        public decimal EstimatedHoursLowerLimit { get; set; }

        public float EstimatedHoursABS { get; set; }

        public int[] Statuses { get; set; }

        IRedmineSync IRedmineOptions.Sync => this;

        public int Interval { get; set; }

        public int UserId { get; set; }
    }
}
