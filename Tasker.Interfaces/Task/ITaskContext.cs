namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskContext
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        TaskKind Kind { get; }

        TaskState Status { get; }

        MergeState MergeStatus { get; }
    }
}
