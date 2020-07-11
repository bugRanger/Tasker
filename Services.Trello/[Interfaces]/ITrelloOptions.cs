namespace Services.Trello
{
    public interface ITrelloOptions 
    {
        string AppKey { get; }

        string Token { get; }

        string BoardId { get; set; }

        string BoardName { get; set; }

        ITrelloSync Sync { get; }
    }
}
