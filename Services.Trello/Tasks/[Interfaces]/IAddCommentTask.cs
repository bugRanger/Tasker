namespace Services.Trello.Tasks
{
    using Common.Tasks;

    public interface IAddCommentTask : ITaskItem<ITrelloVisitor>
    {
        string CardId { get; }

        string Comment { get; }
    }
}