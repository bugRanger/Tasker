namespace Common.Tasks
{
    using System;

    public abstract class TaskItem<TVisitor, TResult> : ITaskItem<TVisitor>
        where TVisitor : ITaskVisitor
    {
        #region Fields

        private ITaskItem<TVisitor> _thenTask;

        #endregion Fields

        #region Properties

        protected Action<TResult> Callback { get; }

        #endregion Properties

        protected TaskItem(Action<TResult> callback)
        {
            Callback = callback;
        }

        protected abstract TResult HandleImpl(TVisitor visitor);

        public void Handle(TVisitor visitor) 
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

        public TaskItem<TVisitor, TOutResult> Then<TOutResult>(TaskItem<TVisitor, TOutResult> task)
        {
            _thenTask = task;
            return task;
        }
    }
}
