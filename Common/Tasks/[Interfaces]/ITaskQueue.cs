namespace Common.Tasks
{
    using System;

    public interface ITaskQueue<TVisitor>
        where TVisitor : ITaskVisitor
    {
        event EventHandler<string> Error;

        void Enqueue(ITaskItem<TVisitor> task);

        void Start();

        void Stop();

        bool HasEnabled();
    }
}
