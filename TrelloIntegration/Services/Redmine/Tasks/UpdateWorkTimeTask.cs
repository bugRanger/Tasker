namespace TrelloIntegration.Services.Redmine.Tasks
{
    using System;

    class UpdateWorkTimeTask : Common.TaskItem<RedmineService, bool>
    {
        public int IssueId { get; }

        public decimal Hours { get; }

        // Max length 255 chars.
        public string Comments { get; }

        public UpdateWorkTimeTask(int issueId, decimal hours, string comments = null, Action<bool> callback = null) : base(callback)
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