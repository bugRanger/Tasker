namespace TrelloIntegration.Common
{
    interface ITaskQueue<TService>
        where TService : ITaskService
    {
        void Enqueue(ITaskItem<TService> task);

        void Start();

        void Stop();

        bool HasEnabled();
    }
}
