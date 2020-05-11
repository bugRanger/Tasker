namespace TrelloIntegration.Common.Tasks
{
    interface ITaskItem<TService>
        where TService : IServiceVisitor
    {
        void Handle(TService service);
    }
}
