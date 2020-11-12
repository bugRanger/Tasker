namespace Tasker.Interfaces.Task
{
    using System;

    public enum TaskState : int
    {
        New = 0,
        InAnalysis,
        InProgress,
        OnReview,
        Resolved,
        Paused,
        Closed,
    }
}
