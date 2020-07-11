namespace Services.Trello
{
    using Common.Command;

    public class CommentEventArgs : CardEventArgs, ICommandArgs
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
