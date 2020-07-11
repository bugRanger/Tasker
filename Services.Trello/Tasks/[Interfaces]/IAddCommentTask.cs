namespace Services.Trello.Tasks
{
    using Common.Tasks;

    public interface IAddCommentTask : ITaskItem<ITrelloService>
    {
        string CardId { get; }
        string Comment { get; }
    }
}