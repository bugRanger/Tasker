﻿namespace Services.Trello.Tasks
{
    using Common.Tasks;

    public interface IUpdateCardTask : ITaskItem<ITrelloService>
    {
        string BoardId { get; }
        string CardId { get; }
        string Description { get; }
        string LabelId { get; }
        string ListId { get; }
        string Subject { get; }
    }
}