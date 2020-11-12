namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskCommon
    {
        string ExternalId { get; }

        ITaskContext Context { get; }
    }
}
