namespace Redmine2Trello.Services
{
    interface ITrelloSync
    {
        int Interval { get; }

        string[] BoardIds { get; }
    }
}
