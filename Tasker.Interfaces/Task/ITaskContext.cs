namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskContext
    {
        string Status { get; }

        string Name { get; }

        string Desc { get; }
    }
}
