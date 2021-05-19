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
        
        private readonly ITaskContainer _container;
        private readonly IDictionary<ITaskService, int> _services;

        #endregion Fields

        #region Constructors

        public TaskController(ITaskContainer container)
        {
            _services = new Dictionary<ITaskService, int>();
            _container = container;
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
                int serviceId = current.Value;
                ITaskService service = current.Key;

                if (owner.Equals(service))
                {
                    cached.GetOrAdd(ownerId, task.ExternalId);
                    continue;
                }

                if (!_container.TryGet(serviceId, task.ExternalId, out var taskCurrent))
                {
                    taskCurrent = new TaskCommon();
                }

                taskCurrent.Context.ApplyPatch(task.Context, properties);

                // TODO: Generation pipeline witch return rollbac/callback operations.
                service.Enqueue(new UpdateTask(taskCurrent, taskId =>
                {
                    cached.GetOrAdd(serviceId, taskId);
                    if (cached.Count == _services.Count)
                    {
                        _container.Set(cached, taskCurrent.Context);
                    }
                }));
            }
        }

        #endregion Methods
    }
}
