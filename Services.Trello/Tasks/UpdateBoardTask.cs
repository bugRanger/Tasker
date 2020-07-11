namespace Services.Trello.Tasks
{
    using System;
    using Common.Tasks;

    public class UpdateBoardTask : TaskItem<ITrelloService, string>, IUpdateBoardTask
    {
        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        public Func<string, bool> СlearСontents { get; }

        public UpdateBoardTask(string name, string id = null, string desc = null, Func<string, bool> clear = null, Action<string> callback = null) : base(callback)
        {
            Id = id;
            Name = name;
            Description = desc;
            СlearСontents = clear;
        }

        protected override string HandleImpl(ITrelloService service)
        {
            return service.Handle(this);
        }
    }
}
