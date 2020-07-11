namespace Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public class TimelineEnviroment : ITimelineEnviroment
    {
        #region Fields

        private static ITimelineEnviroment _instance;

        private ConcurrentDictionary<int, Thread> _threads;

        #endregion Fields

        #region Properies

        public static ITimelineEnviroment Instance
        {
            get 
            {
                if (_instance == null) 
                    _instance = new TimelineEnviroment();

                return _instance;
            }
        }

        #endregion Properies

        #region Constructors

        protected TimelineEnviroment() 
        {
            _threads = new ConcurrentDictionary<int, Thread>();
        }

        #endregion Constructors

        #region Methods

        // TODO Добавить повторы вызова и интервал.
        public long AppendTimer(int interval, Action action)
        {            
            var thread = new Thread(() => action());

            _threads[thread.ManagedThreadId] = thread;

            return thread.ManagedThreadId;
        }

        public void RemoveTimer(long timerId)
        {
            if (!_threads.TryRemove((int)timerId, out var thread))
                return;

            thread.Join();
        }

        public long TickCount()
            => Environment.TickCount64 & Int64.MaxValue;

        #endregion Methods
    }
}
