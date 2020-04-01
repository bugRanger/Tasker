namespace Redmine2Trello.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    class Locker
    {
        #region Fields

        private const int ENABLED = 1;

        private const int DISABLE = 0;

        private int _state;

        #endregion Fields

        #region Constructors

        public Locker()
        {
            _state = DISABLE;
        }

        #endregion Constructors

        #region Methods


        public bool HasEnabled()
        {
            return Interlocked.CompareExchange(ref _state, ENABLED, ENABLED) == ENABLED;
        }

        public bool SetEnabled()
        {
            return Interlocked.CompareExchange(ref _state, ENABLED, DISABLE) == DISABLE;
        }

        public bool SetDisabled()
        {
            return Interlocked.CompareExchange(ref _state, DISABLE, ENABLED) == ENABLED;
        }

        #endregion Methods
    }

    class TaskQueue<TTask>
    {
        #region Fields

        private const int WAIT_DEFAULT = 300;

        private readonly AutoResetEvent _syncTask;

        private ConcurrentQueue<TTask> _queueTask;

        private Locker _locker;
        private Action<TTask> _execute;
        private int _wait;

        private Thread _thread;

        #endregion Fields

        #region Constructor

        public TaskQueue(Action<TTask> execute, int wait = WAIT_DEFAULT)
        {
            _queueTask = new ConcurrentQueue<TTask>();
            _syncTask = new AutoResetEvent(false);
            _locker = new Locker();
            _execute = execute;
            _wait = wait;
            _thread = null;
        }

        #endregion Constructor

        #region Methods

        public void Enqueue(TTask task)
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

        public void HandleTask()
        {
            while (true)
            {
                var startTime = Environment.TickCount64 & Int64.MaxValue;

                try
                {
                    while (_queueTask.TryDequeue(out TTask task))
                    {
                        if (!_locker.HasEnabled())
                            return;

                        _execute?.Invoke(task);
                    }
                }
                catch
                {
                    // Ignore.
                }

                var endTime = Environment.TickCount64 & Int64.MaxValue;
                var sleep = _wait - (endTime - startTime);
                if (sleep > 0)
                    _syncTask.WaitOne((int)sleep);

            }
        }

        #endregion Methods
    }
}
