namespace Redmine2Trello.Services.Trello.Tasks
{
    class ImportIssueTask : Common.TaskItem<TrelloService>
    {
        public string Project { get; }

        public string Subject { get; }

        public string Status { get; }

        public ImportIssueTask(string project, string subject, string status) 
        {
            Project = project;
            Subject = subject;
            Status = status;
        }

        public override void Handle(TrelloService service)
        {
            service.Handle(this);
        }
    }
}
