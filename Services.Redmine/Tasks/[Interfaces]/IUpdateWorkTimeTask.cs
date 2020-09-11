namespace Services.Redmine.Tasks
{
    using Common.Tasks;

    public interface IUpdateWorkTimeTask : ITaskItem<IRedmineVisitor>
    {
        #region Properties

        int IssueId { get; }

        decimal Hours { get; }

        string Comments { get; }

        #endregion Properties
    }
}