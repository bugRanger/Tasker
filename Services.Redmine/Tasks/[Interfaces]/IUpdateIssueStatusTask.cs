namespace Services.Redmine.Tasks
{
    using Common.Tasks;

    public interface IUpdateIssueStatusTask : ITaskItem<IRedmineService>
    {
        #region Properties

        int IssueId { get; }

        int StatusId { get; }
                
        #endregion Properties
    }
}