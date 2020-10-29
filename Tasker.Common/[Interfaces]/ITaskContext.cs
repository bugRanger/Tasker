namespace Tasker.Interfaces
{
    using System;

    public interface ITaskContext
    {
        string Status { get; }

        string Name { get; }

        string Desc { get; }
    }
}
