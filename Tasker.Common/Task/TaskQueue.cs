namespace Tasker.Common.Task
{
    using System;
    using System.Threading;
    using System.Collections.Concurrent;

    using Framework.Common;
    using Framework.Timeline;

    using Tasker.Interfaces.Task;

    public class TaskQueue : ITaskQueue
    {
        #region Fields

        private const int WAIT_DEFAULT = 300;

        private readonly Locker _locker;
        private readonly AutoResetEvent _syncTask;
        private readonly ConcurrentQueue<ITaskItem> _queueTask;
        private readonly Action<ITaskItem> _execute;
        private readonly ITimelineEnvironment _timeline;
        private readonly int _wait;

        private Thread _thread;

        #endregion Fields

        #region Events

        public event Action<ITaskItem, string> Error;

        #endregion Events

        #region Constructor

        public TaskQueue(Action<ITaskItem> execute, ITimelineEnvironment timeline, int wait = WAIT_DEFAULT)
        {
            _queueTask = new ConcurrentQueue<ITaskItem>();
            _syncTask = new AutoResetEvent(false);
            _locker = new Locker();

            _timeline = timeline;
            _execute = execute;
            _wait = wait;
            _thread = null;
        }

        #endregion Constructor

        #region Methods

        public void Enqueue(ITaskItem task)
        {
            if (!_locker.IsEnabled)
                return;

            _queueTask.Enqueue(task);
            _syncTask.Set();
        }

        public void Start()
        {
            if (!_locker.SetEnabled())
                return;

            _thread = new Thread(HandleTask);
            _thread.Start();
        }

        public void Stop()
        {
            if (!_locker.SetDisabled())
                return;

            _thread.Join();
        }

        public bool HasEnabled()
        {
            return _locker.IsEnabled;
        }

        public bool IsEmpty()
        {
            var waiter = new ManualResetEvent(false);

            _queueTask.Enqueue(new SyncActionTask(() => waiter.Set()));
            return waiter.WaitOne();
        }

        private void HandleTask()
        {
            while (_locker.IsEnabled)
            {
                var startTime = _timeline.TickCount;

                while (_queueTask.TryDequeue(out ITaskItem task))
                {
                    if (!_locker.IsEnabled)
                        return;

                    try
                    {
                        _execute?.Invoke(task);
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke(task, ex.Message);
                    }
                }

                var endTime = _timeline.TickCount;
                var sleep = _wait - (endTime - startTime);
                if (sleep > 0)
                    _syncTask.WaitOne((int)sleep);
            }
        }

        #endregion Methods
    }
}
