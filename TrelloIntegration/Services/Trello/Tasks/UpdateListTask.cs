namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateListTask : TaskItem<ITrelloVisitor, string>
    {
        public string BoardId { get; }

        public string ListId { get; }

        public string Name { get; }

        public UpdateListTask(string boardId, string name, string listId = null, Action<string> callback = null) : base(callback)
        {
            BoardId = boardId;
            ListId = listId;
            Name = name;
        }

        protected override string HandleImpl(ITrelloVisitor service) 
        {
            return service.Handle(this);
        }
    }
}
