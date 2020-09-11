namespace Services.Redmine.Tasks
{
    using System;
    using Common.Tasks;

    public class UpdateWorkTimeTask : TaskItem<IRedmineVisitor, bool>, IUpdateWorkTimeTask
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

        protected override bool HandleImpl(IRedmineVisitor visitor)
        {
            return visitor.Handle(this);
        }
    }
}