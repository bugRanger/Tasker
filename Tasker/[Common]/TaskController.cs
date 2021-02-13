namespace Tasker
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Security.Cryptography;

    using Tasker.Interfaces.Task;
    using Tasker.Common.Task;

    public class TaskController : ITaskController
    {
        #region Fields

        private readonly IDictionary<ITaskService, int> _services;
        private readonly IDictionary<int, TaskCommon> _tasks;

        #endregion Fields

        #region Constructors

        public TaskController(IDictionary<int, TaskCommon> tasks)
        {
            _services = new Dictionary<ITaskService, int>();
            _tasks = tasks;
        }

        #endregion Constructors

        #region Methods

        public void Register(ITaskService service)
        {
            if (!_services.TryAdd(service, _services.Count + 1))
                return;

            service.Notify += HandleNotify;
        }


        public void Start()
        {
            foreach (var service in _services.Keys.ToArray())
            {
                service.Start();
            }
        }

        public void Stop()
        {
            foreach (var service in _services.Keys.ToArray().Reverse())
            {
                service.Stop();
            }
        }

        private void HandleNotify(object sender, ITaskCommon task, IEnumerable<string> properties)
        {
            if (!(sender is ITaskService owner) || 
                !_services.TryGetValue(owner, out var ownerId))
            {
                return;
            }

            var cached = new ConcurrentDictionary<int, string>();
            foreach (var current in _services.ToArray())
            {
                if (owner.Equals(current.Key))
                {
                    cached.GetOrAdd(ownerId, task.ExternalId);
                    continue;
                }

                if (!_tasks.TryGetValue(GetKey(current.Value, task.ExternalId), out var taskCurrent))
                {
                    taskCurrent = new TaskCommon();
                }

                taskCurrent.Context.ApplyPatch(task.Context, properties);

                current.Key.Enqueue(new UpdateTask(taskCurrent, taskId =>
                {
                    cached.GetOrAdd(current.Value, taskId);
                    if (cached.Count == _services.Count)
                    {
                        UpdateContainer(_tasks, cached, taskCurrent.Context);
                    }
                }));
            }
        }

        // TODO Move entry container.
        private static void UpdateContainer(IDictionary<int, TaskCommon> container, IReadOnlyDictionary<int, string> cached, TaskContext context)
        {
            foreach (KeyValuePair<int, string> source in cached)
            {
                if (string.IsNullOrWhiteSpace(source.Value))
                {
                    continue;
                }

                foreach (KeyValuePair<int, string> target in cached)
                {
                    if (source.Key == target.Key || string.IsNullOrWhiteSpace(target.Value))
                    {
                        continue;
                    }

                    int keySource = GetKey(source.Key, target.Value);
                    int keyTarget = GetKey(target.Key, source.Value);

                    container[keySource] = new TaskCommon { ExternalId = source.Value, Context = context };
                    container[keyTarget] = new TaskCommon { ExternalId = target.Value, Context = context };
                }
            }
        }

        // TODO Move entry container.
        public static int GetKey(int subjectId, string objectId)
        {
            using var sha = MD5.Create();

            int hash = 17;
            hash = hash * 31 + BitConverter.ToInt32(sha.ComputeHash(Encoding.UTF8.GetBytes(subjectId.ToString())), 0);
            hash = hash * 31 + BitConverter.ToInt32(sha.ComputeHash(Encoding.UTF8.GetBytes(objectId)), 0);

            return hash;
        }

        #endregion Methods
    }
}
