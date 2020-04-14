namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;

    class ImportCardTask : Common.TaskItem<TrelloService, string>
    {
        public string Project { get; }

        public string Subject { get; }

        public string Description { get; }

        public string Status { get; }

        public string[] Statuses { get; } 

        public ImportCardTask(string project, string subject, string description, string status, string[] statuses, Action<string> callback = null) : base(callback)
        {
            Project = project;
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
