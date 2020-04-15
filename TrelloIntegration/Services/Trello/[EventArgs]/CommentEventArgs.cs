namespace TrelloIntegration.Services.Trello
{
    class CommentEventArgs : CardEventArgs
    {
        public string CommentId { get; }

        public string UserId { get; }

        public string Text { get; }

        public CommentEventArgs(string cardId, string commentId, string userId, string text) : base(cardId)
        {
            CommentId = commentId;
            UserId = userId;
            Text = text;
        }
    }
}
