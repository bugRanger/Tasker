namespace Services.Trello.Tasks
{
    using System;

    using Manatee.Trello;
    using Common.Tasks;

    public class EmojiCommentTask : TaskItem<ITrelloService, bool>, IEmojiCommentTask
    {
        public string CardId { get; }

        public string CommentId { get; }

        public Emoji Emoji { get; }

        public EmojiCommentTask(string cardId, string commentId, Emoji emoji, Action<bool> callback = null) : base(callback)
        {
            CardId = cardId;
            CommentId = commentId;
            Emoji = emoji;
        }

        protected override bool HandleImpl(ITrelloService service)
        {
            return service.Handle(this);
        }
    }
}
