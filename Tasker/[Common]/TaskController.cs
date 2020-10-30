namespace Tasker
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

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

            foreach (ITaskService current in _services.ToArray())
            {
                if (sender.Equals(current))
                {
                    continue;
                }

                var item = new TaskCommon
                {
                    Id = _tasks.TryGetValue(GetKey(current, task.Id), out string taskId) ? taskId : null,
                    Context = task.Context,
                };

                current.Enqueue(new UpdateTask(item, taskItemId => UpdateContainer(_tasks, owner, task.Id, current, taskItemId)));
            }
        }

        // TODO Move entry container.
        private static void UpdateContainer(IDictionary<int, string> container, ITaskService source, string sourceTaskId, ITaskService target, string targetTaskId)
        {
            int keySource = GetKey(source, targetTaskId);
            int keyTarget = GetKey(target, sourceTaskId);

            container[keySource] = sourceTaskId;
            container[keyTarget] = targetTaskId;
        }

        // TODO Move entry container.
        private static int GetKey(ITaskService service, string objectId)
        {
            int hash = 17;
            hash = hash * 31 + service.Id.GetHashCode();
            hash = hash * 31 + objectId.GetHashCode();
            return hash;
        }

        #endregion Methods
    }
}
