namespace Tasker.Common
{
    using System;

    using Tasker.Interfaces;

    public class TaskCommon : ITaskCommon
    {
        #region Properties

        public string Id { get; set; }

        public ITaskContext Context { get; set; }

        #endregion Properties
    }
}
