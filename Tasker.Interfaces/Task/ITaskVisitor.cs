namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskVisitor
    {
        string Handle(ITaskCommon task);
    }
}
