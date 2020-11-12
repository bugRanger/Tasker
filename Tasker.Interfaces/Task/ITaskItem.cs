namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskItem
    {
        void Handle(ITaskVisitor visitor);
    }
}
