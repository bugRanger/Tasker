namespace Tasker.Common.Task
{
    using System;
    using System.Threading.Tasks;

    using Tasker.Interfaces.Task;

    public class SyncActionTask : TaskItem<bool>
    {
        #region Constants

        private const int DEFAULT_INTERVAL = 100;

        #endregion Constants

        #region Fields

        private readonly Func<bool> _action;

        private readonly ITaskQueue _queue;

        #endregion Fields

        #region Properties

        public int? Interval { get; }

        #endregion Properties

        public SyncActionTask(SyncActionTask task) : this(task._action, task._queue, task.Interval, task.Callback)
        {
        }

        public SyncActionTask(Func<bool> action, ITaskQueue queue = null, int? interval = null, Action<bool> callback = null) : base(callback)
        {
            Interval = interval;

            _action = action;
            _queue = queue;
        }

        protected override bool HandleImpl(ITaskVisitor visitor)
        {
            try
            {
                return _action?.Invoke() ?? false;
            }
            finally
            {
                if (_queue?.HasEnabled() == true)
                {

                    if (Interval == 0)
                    {
                        _queue.Enqueue(new SyncActionTask(this));
                    }
                    else
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
}
