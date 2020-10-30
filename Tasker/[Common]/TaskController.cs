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
        private readonly IDictionary<int, string> _tasks;

        #endregion Fields

        #region Constructors

        public TaskController(IDictionary<int, string> tasks)
        {
            _services = new HashSet<ITaskService>();
            _tasks = tasks;
        }

        #endregion Constructors

        #region Methods

        public void Register(ITaskService service)
        {
            if (!_services.Add(service))
                return;

            service.Notify += HandleNotify;
        }

        private void HandleNotify(object sender, ITaskCommon task)
        {
            if (!(sender is ITaskService owner))
            {
                return;
            }

            var cached = new ConcurrentDictionary<int, string>();

            foreach (ITaskService current in _services.ToArray())
            {
                if (owner.Equals(current))
                {
                    cached.GetOrAdd(owner.Id, task.Id);
                    continue;
                }

                var item = new TaskCommon
                {
                    Id = _tasks.TryGetValue(GetKey(current.Id, task.Id), out string taskId) ? taskId : null,
                    Context = task.Context,
                };

                current.Enqueue(new UpdateTask(item, taskId =>
                {
                    cached.GetOrAdd(current.Id, taskId);
                    if (cached.Count == _services.Count)
                    {
                        UpdateContainer(_tasks, cached);
                    }
                }));
            }
        }

        // TODO Move entry container.
        private static void UpdateContainer(IDictionary<int, string> container, IReadOnlyDictionary<int, string> cached)
        {
            foreach (KeyValuePair<int, string> source in cached)
            {
                foreach (KeyValuePair<int, string> target in cached)
                {
                    if (source.Key == target.Key)
                    {
                        continue;
                    }

                    int keySource = GetKey(source.Key, target.Value);
                    int keyTarget = GetKey(target.Key, source.Value);

                    container[keySource] = source.Value;
                    container[keyTarget] = target.Value;
                }
            }
        }

        // TODO Move entry container.
        private static int GetKey(int subjectId, string objectId)
        {
            int hash = 17;
            hash = hash * 31 + subjectId.GetHashCode();
            hash = hash * 31 + objectId.GetHashCode();
            return hash;
        }

        #endregion Methods
    }
}
