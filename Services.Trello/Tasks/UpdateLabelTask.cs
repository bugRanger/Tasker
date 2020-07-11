namespace Services.Trello.Tasks
{
    using System;

    using Manatee.Trello;

    using Common.Tasks;

    public class UpdateLabelTask : TaskItem<ITrelloService, string>, IUpdateLabelTask
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

        protected override string HandleImpl(ITrelloService service)
        {
            return service.Handle(this);
        }
    }
}
