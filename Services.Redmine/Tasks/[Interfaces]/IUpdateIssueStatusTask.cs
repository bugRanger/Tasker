namespace Services.Redmine.Tasks
{
    using Common.Tasks;

    public interface IUpdateIssueStatusTask : ITaskItem<IRedmineVisitor>
    {
        #region Properties

        int IssueId { get; }

        int StatusId { get; }
                
        #endregion Properties
    }
}