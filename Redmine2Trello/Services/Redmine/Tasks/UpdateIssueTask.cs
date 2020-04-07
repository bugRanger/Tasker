namespace Redmine2Trello.Services.Redmine.Tasks
{
    using Common;
    using System;

    class UpdateIssueTask : TaskItem<RedmineService>
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
