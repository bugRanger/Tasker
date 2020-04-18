namespace TrelloIntegration.Common.Tasks
{
    interface ITaskItem<TService>
        where TService : ITaskService
    {
        void Handle(TService service);
    }
}
