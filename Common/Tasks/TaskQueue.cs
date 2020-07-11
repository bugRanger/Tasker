namespace Common.Tasks
{
    using System;
    using System.Threading;
    using System.Collections.Concurrent;

    public class TaskQueue<TVisitor> : ITaskQueue<TVisitor>
        where TVisitor : ITaskVisitor
    {
        #region Fields

        private const int WAIT_DEFAULT = 300;

        private readonly Locker _locker;
        private readonly AutoResetEvent _syncTask;
        private readonly ConcurrentQueue<ITaskItem<TVisitor>> _queueTask;
        private readonly Action<ITaskItem<TVisitor>> _execute;
        private readonly ITimelineEnviroment _timeline;
        private readonly int _wait;

        private Thread _thread;

        #endregion Fields

        #region Events

        public event EventHandler<string> Error;

        #endregion Events

        #region Constructor

        public TaskQueue(Action<ITaskItem<TVisitor>> execute, ITimelineEnviroment timeline, int wait = WAIT_DEFAULT)
        {
            _queueTask = new ConcurrentQueue<ITaskItem<TVisitor>>();
            _syncTask = new AutoResetEvent(false);
            _locker = new Locker();

            _timeline = timeline;
            _execute = execute;
            _wait = wait;
            _thread = null;
        }

        #endregion Constructor

        #region Methods

        public void Enqueue(ITaskItem<TVisitor> task)
        {
            if (!_locker.HasEnabled())
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
            return _locker.HasEnabled();
        }

        private void HandleTask()
        {
            while (_locker.HasEnabled())
            {
                var startTime = _timeline.TickCount();

                try
                {
                    while (_queueTask.TryDequeue(out ITaskItem<TVisitor> task))
                    {
                        if (!_locker.HasEnabled())
                            return;

                        _execute?.Invoke(task);
                    }
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, ex.Message);
                }

                var endTime = _timeline.TickCount();
                var sleep = _wait - (endTime - startTime);
                if (sleep > 0)
                    _syncTask.WaitOne((int)sleep);
            }
        }

        #endregion Methods
    }
}
