namespace TrelloIntegration.Services.Redmine.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateIssueTask : TaskItem<RedmineService, bool>
    {
        public int IssueId { get; }

        public int StatusId { get; }

        public UpdateIssueTask(int issueId, int statusId, Action<bool> callback = null) : base(callback)
        {
            IssueId = issueId;
            StatusId = statusId;
        }

        protected override bool HandleImpl(RedmineService service)
        {
            return service.Handle(this);
        }
    }
}
