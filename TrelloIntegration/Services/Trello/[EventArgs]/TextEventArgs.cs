namespace TrelloIntegration.Services.Trello
{
    class TextEventArgs : CardEventArgs
    {
        public string Text { get; }

        public string UserId { get; }

        public TextEventArgs(string cardId, string text, string userId) : base(cardId)
        {
            Text = text;
            UserId = userId;
        }
    }
}
