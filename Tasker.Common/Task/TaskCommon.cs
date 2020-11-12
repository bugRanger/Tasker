namespace Tasker.Common.Task
{
    using System;

    using Tasker.Interfaces.Task;

    public class TaskCommon : ITaskCommon, IEquatable<ITaskCommon>
    {
        #region Properties

        public string ExternalId { get; set; }

        public TaskContext Context { get; set; }

        ITaskContext ITaskCommon.Context => Context;

        #endregion Properties

        #region Constructors

        public TaskCommon() 
        {
            Context = new TaskContext();
        }

        #endregion Constructors

        #region Methods

        public bool Equals(ITaskCommon other)
        {
            return other != null
                && ExternalId == other.ExternalId
                && Context.Equals(other.Context);
        }

        #endregion Methods
    }
}
