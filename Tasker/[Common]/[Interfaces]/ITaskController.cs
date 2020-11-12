namespace Tasker
{
    using System;

    using Tasker.Interfaces.Task;

    public interface ITaskController
    {
        #region Methods

        void Register(ITaskService service);

        void Start();

        void Stop();

        #endregion Methods
    }
}
