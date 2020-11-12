namespace Tasker.Common.Task
{
    using System;
    using System.Collections.Generic;

    using Tasker.Interfaces.Task;

    public class UpdateTask : TaskItem<string>, ITaskCommon
    {
        #region Fields

        private readonly ITaskCommon _task;

        #endregion Fields

        #region Properties

        public string ExternalId => _task.ExternalId;

        public ITaskContext Context => _task.Context;

        #endregion Properties

        #region Constructors

        public UpdateTask(ITaskCommon task, Action<string> callback = null) : base(callback)
        {
            _task = task;
        }

        #endregion Constructors

        #region Methods

        protected override string HandleImpl(ITaskVisitor visitor)
        {
            return visitor.Handle(this);
        }

        #endregion Methods
    }
}
