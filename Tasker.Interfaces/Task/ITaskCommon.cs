namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskCommon
    {
        string Id { get; }

        ITaskContext Context { get; }
    }
}
