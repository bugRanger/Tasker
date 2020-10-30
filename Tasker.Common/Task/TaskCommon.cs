namespace Tasker.Common.Task
{
    using System;

    using Tasker.Interfaces.Task;

    public class TaskCommon : ITaskCommon
    {
        #region Properties

        public string Id { get; set; }

        public ITaskContext Context { get; set; }

        #endregion Properties
    }
}
