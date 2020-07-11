namespace Services.Trello.Tasks
{
    using Common.Tasks;

    using Manatee.Trello;

    public interface IUpdateLabelTask : ITaskItem<ITrelloService>
    {
        string BoardId { get; }
        LabelColor? Color { get; }
        string Id { get; }
        string Name { get; }
    }
}