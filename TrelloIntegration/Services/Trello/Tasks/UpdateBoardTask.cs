namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateBoardTask : TaskItem<TrelloService, string>
    {
        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        public UpdateBoardTask(string name, string id = null, string desc = null, Action<string> callback = null) : base(callback)
        {
            Id = id;
            Name = name;
            Description = desc;
        }

        protected override string HandleImpl(TrelloService service)
        {
            return service.Handle(this);
        }
    }
}
