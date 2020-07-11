namespace Services.Redmine.Tasks
{
    using System;
    using Common.Tasks;

    public class UpdateIssueTask : TaskItem<IRedmineService, bool>, IUpdateIssueTask
    {
        public int IssueId { get; }

        public int StatusId { get; }

        public UpdateIssueTask(int issueId, int statusId, Action<bool> callback = null) : base(callback)
        {
            IssueId = issueId;
            StatusId = statusId;
        }

        protected override bool HandleImpl(IRedmineService service)
        {
            return service.Handle(this);
        }
    }
}
