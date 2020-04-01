namespace Redmine2Trello.Services
{
    interface ITrelloOptions 
    {
        string AppKey { get; }

        string Token { get; }

        ITrelloSync Sync { get; }
    }
}
