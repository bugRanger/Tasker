namespace Redmine2Trello.Common
{
    abstract class TaskItem<TService>
        where TService : TaskService
    {
        public abstract void Handle(TService service);
    }
}
