namespace Redmine2Trello.Common
{
    using System;

    abstract class TaskItem<TService>
        where TService : TaskService
    {
        public Action<bool> Callback { get; }

        protected TaskItem(Action<bool> callback)
        {
            Callback = callback;
        }

        public abstract bool HandleImpl(TService service);

        public void Handle(TService service) 
        {
            var result = false;
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
