namespace Common.Tasks
{
    public interface ITaskItem<TVisitor>
        where TVisitor : ITaskVisitor
    {
        void Handle(TVisitor visitor);
    }
}
