namespace TrelloIntegration.Common.Tasks
{
    using System;
    using System.Threading.Tasks;

    class SyncActionTask<T> : TaskItem<T, bool>
        where T : IServiceVisitor
    {
        private const int DEFAULT_INTERVAL = 100;

        public int? Interval { get; }

        public ITaskQueue<T> Queue { get; }

        public Func<bool> Action { get; }

        public SyncActionTask(SyncActionTask<T> task) : this(task.Action, task.Queue, task.Interval, task.Callback)
        {
        }

        public SyncActionTask(Func<bool> action, ITaskQueue<T> queue, int? interval = null, Action<bool> callback = null) : base(callback)
        {
            Interval = interval;
            Queue = queue;
            Action = action;
        }

        protected override bool HandleImpl(T service)
        {
            try
            {
                return Action?.Invoke() ?? false;
            }
            finally
            {
                if (Queue.HasEnabled())
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(Interval ?? DEFAULT_INTERVAL);
                        Queue.Enqueue(new SyncActionTask<T>(this));
                    });
                }
            }
        }
    }
}
