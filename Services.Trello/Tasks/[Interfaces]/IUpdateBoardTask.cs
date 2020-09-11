namespace Services.Trello.Tasks
{
    using System;

    using Common.Tasks;

    public interface IUpdateBoardTask : ITaskItem<ITrelloVisitor>
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        Func<string, bool> СlearСontents { get; }
    }
}