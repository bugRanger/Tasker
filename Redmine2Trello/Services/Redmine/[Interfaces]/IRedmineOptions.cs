namespace Redmine2Trello.Services
{
    interface IRedmineOptions
    {
        string Host { get; }

        string ApiKey { get; }

        IRedmineSync Sync { get; }
    }
}
