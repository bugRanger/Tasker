namespace TrelloIntegration.Common
{
    interface ITaskItem<TService>
        where TService : ITaskService
    {
        void Handle(TService service);
    }
}
