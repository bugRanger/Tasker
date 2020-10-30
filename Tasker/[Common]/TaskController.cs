namespace Tasker
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    using Tasker.Interfaces.Task;
    using Tasker.Common.Task;

    public class TaskController : ITaskController
    {
        #region Fields

        private readonly HashSet<ITaskService> _services;
        private readonly ConcurrentDictionary<int, string> _cached;

        #endregion Fields

        #region Properties

        public IEnumerable<KeyValuePair<int, string>> Cached => _cached;

        #endregion Properties

        #region Constructors

        public TaskController()
        {
            _services = new HashSet<ITaskService>();
            _cached = new ConcurrentDictionary<int, string>();
        }

        #endregion Constructors

        #region Methods

        public void Register(ITaskService service)
        {
            if (!_services.Add(service))
                return;

            service.Notify += HandleNotify;
        }

        public int Load(IEnumerable<KeyValuePair<int, string>> cached)
        {
            int success = 0;

            foreach (var item in cached)
            {
                if (_cached.TryAdd(item.Key, item.Value))
                    success++;
            }

            return success;
        }

        private void HandleNotify(object sender, ITaskCommon task)
        {
            if (!(sender is ITaskService owner))
            {
                return;
            }

            foreach (ITaskService current in _services.ToArray())
            {
                if (sender.Equals(current))
                {
                    continue;
                }

                int key = GetKey(current, task.Id);
                var item = new TaskCommon
                {
                    Id = _cached.TryGetValue(key, out string taskId) ? taskId : null,
                    Context = task.Context,
                };

                current.Enqueue(new UpdateTask(item, taskItemId =>
                {
                    if (string.IsNullOrWhiteSpace(taskItemId))
                    {
                        // TODO Action and write log.
                        return;
                    }

                    int keyOwner = GetKey(owner, taskItemId);
                    int keyCurrent = GetKey(current, task.Id);

                    if (key != keyCurrent)
                    {
                        _cached.Remove(key, out _);
                    }

                    _cached[keyOwner] = task.Id;
                    _cached[keyCurrent] = taskItemId;
                }));
            }
        }

        private int GetKey(ITaskService service, string taskId)
        {
            int hash = 17;
            hash = hash * 31 + service.GetHashCode();
            hash = hash * 31 + taskId.GetHashCode();
            return hash;
        }

        #endregion Methods
    }
}
