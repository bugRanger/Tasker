namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateCardTask : TaskItem<TrelloService, string>
    {
        public string BoardId { get; }

        public string Subject { get; }

        public string Description { get; }

        public string Status { get; }

        public string[] Statuses { get; } 

        public UpdateCardTask(string boardId, string subject, string description, string status, string[] statuses, Action<string> callback = null) : base(callback)
        {
            BoardId = boardId;
            Subject = subject;
            Description = description;
            Status = status;
            Statuses = statuses;
        }

        protected override string HandleImpl(TrelloService service)
        {
            return service.Handle(this);
        }
    }
}
