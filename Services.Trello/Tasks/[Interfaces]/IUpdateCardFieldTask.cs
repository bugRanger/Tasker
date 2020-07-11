namespace Services.Trello.Tasks
{
    using Common.Tasks;

    public interface IUpdateCardFieldTask : ITaskItem<ITrelloService>
    {
        string CardId { get; }
        string FieldId { get; }
        object Value { get; }
    }
}