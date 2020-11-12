namespace Tasker.Common.Task
{
    using System;

    using Tasker.Interfaces.Task;

    public abstract class TaskItem<TResult> : ITaskItem
    {
        #region Fields

        private ITaskItem _thenTask;

        #endregion Fields

        #region Properties

        public Action<TResult> Callback { get; }

        #endregion Properties

        protected TaskItem(Action<TResult> callback)
        {
            Callback = callback;
        }

        protected abstract TResult HandleImpl(ITaskVisitor visitor);

        public void Handle(ITaskVisitor visitor) 
        {
            TResult result = default;
            try
            {
                result = HandleImpl(visitor);
            }
            finally
            {
                Callback?.Invoke(result);
                _thenTask?.Handle(visitor);
            }
        }

        public TaskItem<TOutResult> Then<TOutResult>(TaskItem<TOutResult> task)
            where TOutResult : TaskItem<TOutResult>
        {
            _thenTask = task;
            return task;
        }
    }
}
