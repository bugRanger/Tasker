namespace Services.Trello.Tasks
{
    using System;

    using Common.Tasks;
    using Tasker.Interfaces;

    public class UpdateCardTask : TaskItem<ITrelloVisitor, string>
    {
        #region Properties

        public string CardId { get; }

        public ITaskContext Context { get; }

        #endregion Properties

        public UpdateCardTask(ITaskCommon task, Action<string> callback = null) : base(callback)
        {
            CardId = task.Id;
            Context = task.Context;
        }

        protected override string HandleImpl(ITrelloVisitor visitor)
        {
            return visitor.Handle(this);
        }
    }
}
