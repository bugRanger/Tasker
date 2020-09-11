namespace Services.Trello.Tasks
{
    using Common.Tasks;

    public interface IUpdateListTask : ITaskItem<ITrelloVisitor>
    {
        string BoardId { get; }

        string ListId { get; }

        string Name { get; }
    }
}