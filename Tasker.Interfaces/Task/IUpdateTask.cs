namespace Tasker.Interfaces.Task
{
    using System;

    public interface IUpdateTask
    {
        #region Properties

        public string Id { get; }

        public ITaskContext Context { get; }


        #endregion Properties
    }
}