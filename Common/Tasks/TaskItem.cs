namespace Common.Tasks
{
    using System;

    public abstract class TaskItem<TVisitor, TResult> : ITaskItem<TVisitor>
        where TVisitor : ITaskVisitor
    {
        protected Action<TResult> Callback { get; }

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
            }
        }
    }
}
