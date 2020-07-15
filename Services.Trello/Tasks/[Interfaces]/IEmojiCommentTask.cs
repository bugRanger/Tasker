﻿namespace Services.Trello.Tasks
{
    using Common.Tasks;

    using Manatee.Trello;

    public interface IEmojiCommentTask : ITaskItem<ITrelloService>
    {
        string CardId { get; }
        string CommentId { get; }
        Emoji Emoji { get; }
    }
}