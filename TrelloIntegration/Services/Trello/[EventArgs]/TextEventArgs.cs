namespace TrelloIntegration.Services.Trello
{
    class TextEventArgs : CardEventArgs
    {
        public string Text { get; }

        public bool IsMy { get; }

        public TextEventArgs(string cardId, string text, bool isMy) : base(cardId)
        {
            Text = text;
            IsMy = isMy;
        }
    }
}
