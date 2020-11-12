namespace Services.Redmine
{
    public interface IRedmineOptions
    {
        #region Properties

        string Host { get; }

        string ApiKey { get; }

        decimal EstimatedHoursLowerLimit { get; }

        float EstimatedHoursABS { get; }

        int[] Statuses { get; }

        IRedmineSync Sync { get; }

        #endregion Properties
    }
}
