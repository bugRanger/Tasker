namespace Services.Redmine.Tasks
{
    using System;
    using Common.Tasks;

    public class UpdateIssueStatusTask : TaskItem<IRedmineVisitor, bool>, IUpdateIssueStatusTask
    {
        #region Properties

        public int IssueId { get; }

        public int StatusId { get; }

        #endregion Properties

        #region Constructors

        public UpdateIssueStatusTask(int issueId, int statusId, Action<bool> callback = null) : base(callback)
        {
            IssueId = issueId;
            StatusId = statusId;
        }

        #endregion Constructors

        #region Methods

        protected override bool HandleImpl(IRedmineVisitor visitor)
        {
            return visitor.Handle(this);
        }

        #endregion Methods
    }
}
