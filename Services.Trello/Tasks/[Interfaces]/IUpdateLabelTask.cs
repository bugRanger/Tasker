namespace Services.Trello.Tasks
{
    using Common.Tasks;

    using Manatee.Trello;

    public interface IUpdateLabelTask : ITaskItem<ITrelloVisitor>
    {
        string Id { get; }

        string Name { get; }

        string BoardId { get; }

        LabelColor? Color { get; }
    }
}