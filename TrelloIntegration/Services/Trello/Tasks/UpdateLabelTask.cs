namespace TrelloIntegration.Services.Trello.Tasks
{
    using Manatee.Trello;
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateLabelTask : TaskItem<ITrelloVisitor, string>
    {
        public string BoardId { get; }

        public string Id { get; }

        public string Name { get; }

        public LabelColor? Color { get; }

        public UpdateLabelTask(string boardId, string name, string id = null, LabelColor? color = null, Action<string> callback = null) : base(callback)
        {
            BoardId = boardId;
            Id = id;
            Name = name;
            Color = color;
        }

        protected override string HandleImpl(ITrelloVisitor service) 
        {
            return service.Handle(this);
        }
    }
}
