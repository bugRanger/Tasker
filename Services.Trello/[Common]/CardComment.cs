namespace Services.Trello
{
    using Common.Command;

    public class CardComment : ICommandArgs
    {
        public string CardId { get; }

        public string CommentId { get; }

        public string UserId { get; }

        public string Text { get; }

        public CardComment(string cardId, string commentId, string userId, string text)
        {
            CardId = cardId;
            CommentId = commentId;
            UserId = userId;
            Text = text;
        }
    }
}
