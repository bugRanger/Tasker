namespace Services.Trello.Tasks
{
    using System;

    using Common.Tasks;

    public interface IUpdateBoardTask : ITaskItem<ITrelloService>
    {
        string Description { get; }
        string Id { get; }
        string Name { get; }
        Func<string, bool> СlearСontents { get; }
    }
}