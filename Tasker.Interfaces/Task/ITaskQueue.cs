namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskQueue
    {
        event Action<ITaskItem, string> Error;

        void Enqueue(ITaskItem task);

        void Start();

        void Stop();

        bool HasEnabled();
    }
}
