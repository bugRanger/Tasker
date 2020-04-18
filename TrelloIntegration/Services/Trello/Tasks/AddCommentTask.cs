namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;

    using TrelloIntegration.Common.Tasks;

    class AddCommentTask : TaskItem<TrelloService, bool>
    {
        public string CardId { get; }

        public string Comment { get; }

        public AddCommentTask(string cardId, string comment, Action<bool> callback = null) : base(callback)
        {
            CardId = cardId;
            Comment = comment;
        }

        protected override bool HandleImpl(TrelloService service)
        {
            return service.Handle(this);
        }
    }
}
