namespace Tasker.Interfaces.Task
{
    using System;
    using System.Collections.Generic;

    public interface ITaskService 
    {
        #region Events

        event Action<object, ITaskCommon, IEnumerable<string>> Notify;

        #endregion Events

        #region Methods

        void Start();

        void Stop();

        void Enqueue(ITaskItem task);

        #endregion Methods
    }
}
