namespace Tasker.Interfaces
{
    using System;

    public interface ITaskCommon
    {
        string Id { get; }

        ITaskContext Context { get; }
    }
}
