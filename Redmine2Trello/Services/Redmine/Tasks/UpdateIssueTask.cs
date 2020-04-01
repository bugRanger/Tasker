namespace Redmine2Trello.Services.Redmine.Tasks
{
    using Common;

    class UpdateIssueTask : TaskItem<RedmineService>
    {
        public int IssueId { get; set; }

        public int StatusId { get; set; }

        public override void Handle(RedmineService service)
        {
            service.Handle(this);
        }
    }
}
