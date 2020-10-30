namespace Tasker.Interfaces.Task
{
    using System;

    public interface ITaskService 
    {
        #region Properties

        int Id { get; }

        #endregion Properties

        #region Events

        event Action<object, ITaskCommon> Notify;

        #endregion Events

        #region Methods

        void Start();

        void Stop();

        void Enqueue(ITaskItem task);

        #endregion Methods
    }
}
