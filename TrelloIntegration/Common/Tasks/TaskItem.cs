namespace TrelloIntegration.Common.Tasks
{
    using System;

    abstract class TaskItem<TService, TResult> : ITaskItem<TService>
        where TService : IServiceVisitor
    {
        protected Action<TResult> Callback { get; }

        protected TaskItem(Action<TResult> callback)
        {
            Callback = callback;
        }

        protected abstract TResult HandleImpl(TService service);

        public void Handle(TService service) 
        {
            TResult result = default;
            try
            {
                result = HandleImpl(service);
            }
            finally
            {
                Callback?.Invoke(result);
            }
        }
    }
}
