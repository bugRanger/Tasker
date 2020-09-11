namespace Services.Trello.Tasks
{
    using System;

    using Common.Tasks;

    public class AddCommentTask : TaskItem<ITrelloVisitor, bool>, IAddCommentTask
    {
        public string CardId { get; }

        public string Comment { get; }

        public AddCommentTask(string cardId, string comment, Action<bool> callback = null) : base(callback)
        {
            CardId = cardId;
            Comment = comment;
        }

        protected override bool HandleImpl(ITrelloVisitor visitor)
        {
            return visitor.Handle(this);
        }
    }
}
