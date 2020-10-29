namespace Tasker.Interfaces
{
    using System;

    using Tasker.Common;

    public interface ITaskService 
    {
        #region Events

        event Action<object, ITaskCommon, NotifyAction> Notify;

        #endregion Events

        #region Methods

        void Start();

        void Stop();

        void Enqueue(ITaskCommon task, NotifyAction action, Action<ITaskCommon> callback = null);

        #endregion Methods
    }
}
