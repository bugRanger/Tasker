namespace Tasker.Common.Task
{
    using System;
    using System.Threading.Tasks;

    using Tasker.Interfaces.Task;

    public class SyncActionTask : TaskItem<bool>
    {
        private const int DEFAULT_INTERVAL = 100;

        private Func<bool> Action { get; }

        private readonly ITaskQueue _queue;

        public int? Interval { get; }

        public SyncActionTask(SyncActionTask task) : this(task.Action, task._queue, task.Interval, task.Callback)
        {
        }

        public SyncActionTask(Func<bool> action, ITaskQueue queue = null, int? interval = null, Action<bool> callback = null) : base(callback)
        {
            Interval = interval;
            Action = action;

            _queue = queue;
        }

        protected override bool HandleImpl(ITaskVisitor visitor)
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
                        _queue.Enqueue(new SyncActionTask(this));
                    });
                }
            }
        }
    }
}
