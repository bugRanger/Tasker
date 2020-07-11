namespace Services.Trello.Tasks
{
    using System;

    using Common.Tasks;

    public class UpdateListTask : TaskItem<ITrelloService, string>, IUpdateListTask
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

        protected override string HandleImpl(ITrelloService service)
        {
            return service.Handle(this);
        }
    }
}
