namespace Common.Tasks
{
    using System;
    using System.Threading.Tasks;

    public class SyncActionTask<T> : TaskItem<T, bool>
        where T : ITaskVisitor
    {
        private const int DEFAULT_INTERVAL = 100;

        private readonly ITaskQueue<T> _queue;

        public int? Interval { get; }

        public Func<bool> Action { get; }

        public SyncActionTask(SyncActionTask<T> task) : this(task.Action, task._queue, task.Interval, task.Callback)
        {
        }

        public SyncActionTask(Func<bool> action, ITaskQueue<T> queue = null, int? interval = null, Action<bool> callback = null) : base(callback)
        {
            Interval = interval;
            Action = action;

            _queue = queue;
        }

        protected override bool HandleImpl(T service)
        {
            try
            {
                return Action?.Invoke() ?? false;
            }
            finally
            {
                if (_queue?.HasEnabled() == true)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(Interval ?? DEFAULT_INTERVAL);
                        _queue.Enqueue(new SyncActionTask<T>(this));
                    });
                }
            }
        }
    }
}
