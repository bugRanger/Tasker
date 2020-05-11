namespace TrelloIntegration.Common.Tasks
{
    using System;

    interface ITaskQueue<TService>
        where TService : IServiceVisitor
    {
        event EventHandler<string> Error;

        void Enqueue(ITaskItem<TService> task);

        void Start();

        void Stop();

        bool HasEnabled();
    }
}
