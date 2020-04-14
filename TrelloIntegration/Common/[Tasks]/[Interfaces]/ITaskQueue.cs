namespace TrelloIntegration.Common
{
    using System;

    interface ITaskQueue<TService>
        where TService : ITaskService
    {
        event EventHandler<string> Error;

        void Enqueue(ITaskItem<TService> task);

        void Start();

        void Stop();

        bool HasEnabled();
    }
}
