namespace TrelloIntegration.Services
{
    interface IRedmineOptions
    {
        string Host { get; }

        string ApiKey { get; }

        float EstimatedHoursABS { get; }

        IRedmineSync Sync { get; }
    }
}
