namespace Redmine2Trello.Services.Redmine.Tasks
{
    using Common;

    class UpdateIssueTask : TaskItem<RedmineService>
    {
        public int IssueId { get; }

        public int StatusId { get; }

        public UpdateIssueTask(int issueId, int statusId) 
        {
            IssueId = issueId;
            StatusId = statusId;
        }

        public override void Handle(RedmineService service)
        {
            service.Handle(this);
        }
    }
}
