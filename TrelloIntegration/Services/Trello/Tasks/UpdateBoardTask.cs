﻿namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateBoardTask : TaskItem<ITrelloVisitor, string>
    {
        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        public Func<string, bool> СlearСontents { get; }

        public UpdateBoardTask(string name, string id = null, string desc = null, Func<string, bool> clear = null, Action<string> callback = null) : base(callback)
        {
            Id = id;
            Name = name;
            Description = desc;
            СlearСontents = clear;
        }

        protected override string HandleImpl(ITrelloVisitor service)
        {
            return service.Handle(this);
        }
    }
}
