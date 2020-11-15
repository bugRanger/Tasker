namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskItem
    {
        #region Properties

        long? Interval { get;  }

        long LastTime { get; set; }

        #endregion Properties

        #region Methods

        void Handle(ITaskVisitor visitor);

        #endregion Methods
    }
}
