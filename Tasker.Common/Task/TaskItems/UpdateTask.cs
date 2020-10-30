namespace Tasker.Common.Task
{
    using System;

    using Tasker.Interfaces.Task;

    public class UpdateTask : TaskItem<string>, IUpdateTask
    {
        #region Properties

        public string Id { get; }

        public ITaskContext Context { get; }

        #endregion Properties

        public UpdateTask(ITaskCommon task, Action<string> callback = null) : base(callback)
        {
            Id = task.Id;
            Context = task.Context;
        }

        protected override string HandleImpl(ITaskVisitor visitor)
        {
            return visitor.Handle(this);
        }
    }
}
