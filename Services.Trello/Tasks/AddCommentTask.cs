namespace Services.Trello.Tasks
{
    using System;

    using Common.Tasks;

    public class AddCommentTask : TaskItem<ITrelloService, bool>, IAddCommentTask
    {
        public string CardId { get; }

        public string Comment { get; }

        public AddCommentTask(string cardId, string comment, Action<bool> callback = null) : base(callback)
        {
            CardId = cardId;
            Comment = comment;
        }

        protected override bool HandleImpl(ITrelloService service)
        {
            return service.Handle(this);
        }
    }
}
