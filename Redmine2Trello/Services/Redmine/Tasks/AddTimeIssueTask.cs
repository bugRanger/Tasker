namespace Redmine2Trello.Services.Redmine.Tasks
{
    using System;

    class AddTimeIssueTask : Common.TaskItem<RedmineService>
    {
        public int IssueId { get; }

        public decimal Hours { get; }

        // Max length 255 chars.
        public string Comments { get; }

        public AddTimeIssueTask(int issueId, decimal hours, string comments = null, Action<bool> callback = null) : base(callback)
        {
            IssueId = issueId;
            Hours = hours;
            Comments = comments;
        }

        protected override bool HandleImpl(RedmineService service)
        {
            return service.Handle(this);
        }
    }
}