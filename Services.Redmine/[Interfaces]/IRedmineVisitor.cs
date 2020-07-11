namespace Services.Redmine
{
    using Common.Tasks;
    using Services.Redmine.Tasks;

    public interface IRedmineService : ITaskVisitor
    {

        bool Handle(IUpdateWorkTimeTask task);

        bool Handle(IUpdateIssueTask task);
    }
}
