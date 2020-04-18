namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;

    using Manatee.Trello;
    using TrelloIntegration.Common.Tasks;

    class EmojiCommentTask : TaskItem<TrelloService, bool>
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

        protected override bool HandleImpl(TrelloService service)
        {
            return service.Handle(this);
        }
    }
}
