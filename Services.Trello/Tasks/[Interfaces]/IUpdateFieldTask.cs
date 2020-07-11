namespace Services.Trello.Tasks
{
    using Common.Tasks;

    using Manatee.Trello;

    public interface IUpdateFieldTask : ITaskItem<ITrelloService>
    {
        string BoardId { get; }
        string Id { get; }
        string Name { get; }
        IDropDownOption[] Options { get; }
        CustomFieldType Type { get; }
    }
}