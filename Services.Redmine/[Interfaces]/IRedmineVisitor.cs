namespace Services.Redmine
{
    using Common.Tasks;
    using Tasks;

    public interface IRedmineVisitor : ITaskVisitor
    {

        bool Handle(IUpdateWorkTimeTask task);

        bool Handle(IUpdateIssueStatusTask task);
    }
}
