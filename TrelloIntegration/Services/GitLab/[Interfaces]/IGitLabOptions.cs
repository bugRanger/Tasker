namespace TrelloIntegration.Services
{
    interface IGitLabOptions
    {
        string Host { get; }

        string Token { get; }

        IGitLabSync Sync { get; }
    }
}
