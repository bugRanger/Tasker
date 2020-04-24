namespace TrelloIntegration.Services
{
    interface IRedmineOptions
    {
        string Host { get; }

        string ApiKey { get; }

        float EstimatedHoursABS { get; }

        int[] Statuses { get; }

        IRedmineSync Sync { get; }
    }
}
