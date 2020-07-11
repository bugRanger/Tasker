namespace Services.Trello.Tasks
{
    using Common.Tasks;

    public interface IUpdateListTask : ITaskItem<ITrelloService>
    {
        string BoardId { get; }
        string ListId { get; }
        string Name { get; }
    }
}