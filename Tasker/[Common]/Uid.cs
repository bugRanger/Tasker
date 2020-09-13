namespace Tasker
{
    using System;
    using System.Collections.Concurrent;

    public class Uid<T> : IUid<T>
    {
        #region Fields

        private readonly ConcurrentDictionary<ServiceType, T> _mapper;

        #endregion Fields

        #region Properties

        public T this[ServiceType type] => _mapper[type];

        #endregion Properties

        #region Constructors

        public Uid() 
        {
            _mapper = new ConcurrentDictionary<ServiceType, T>();
            _mapper.TryAdd(ServiceType.Trello, default);
            _mapper.TryAdd(ServiceType.Gitlab, default);
            _mapper.TryAdd(ServiceType.Redmine, default);
        }

        #endregion Constructors
    }
}
