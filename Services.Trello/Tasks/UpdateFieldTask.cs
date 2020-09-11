namespace Services.Trello.Tasks
{
    using System;

    using Manatee.Trello;

    using Common.Tasks;

    public class UpdateFieldTask : TaskItem<ITrelloVisitor, string>, IUpdateFieldTask
    {
        public string BoardId { get; }

        public string Id { get; }

        public string Name { get; }

        public CustomFieldType Type { get; }

        public IDropDownOption[] Options { get; }

        public UpdateFieldTask(string boardId, string name, string id = null, CustomFieldType type = CustomFieldType.Unknown,
            IDropDownOption[] options = null, Action<string> callback = null) : base(callback)
        {
            BoardId = boardId;
            Id = id;
            Name = name;
            Type = type;
            Options = options;
        }

        protected override string HandleImpl(ITrelloVisitor visitor)
        {
            return visitor.Handle(this);
        }
    }
}
