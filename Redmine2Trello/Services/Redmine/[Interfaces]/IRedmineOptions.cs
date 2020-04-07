namespace Redmine2Trello.Services
{
    interface IRedmineOptions
    {
        string Host { get; }

        string ApiKey { get; }

        float EstimatedHoursABS { get; }

        IRedmineSync Sync { get; }
    }
}
