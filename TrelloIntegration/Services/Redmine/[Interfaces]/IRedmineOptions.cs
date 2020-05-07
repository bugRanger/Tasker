namespace TrelloIntegration.Services
{
    interface IRedmineOptions
    {
        string Host { get; }

        string ApiKey { get; }

        decimal EstimatedHoursLowerLimit { get; }

        float EstimatedHoursABS { get; }

        int[] Statuses { get; }

        IRedmineSync Sync { get; }
    }
}
