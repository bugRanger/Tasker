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

        public TaskController(IDictionary<int, string> taskContainer)
        {
            _services = new HashSet<ITaskService>();
            _tasks = taskContainer;
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

                int key = GetKey(current, task.Id);
                var item = new TaskCommon
                {
                    Id = _tasks.TryGetValue(key, out string taskId) ? taskId : null,
                    Context = task.Context,
                };

                current.Enqueue(new UpdateTask(item, taskItemId => UpdateTaskId(owner, task.Id, current, taskItemId)));
            }
        }

        private void UpdateTaskId(ITaskService source, string sourceTaskId, ITaskService target, string targetTaskId)
        {
            int keySource = GetKey(source, targetTaskId);
            int keyTarget = GetKey(target, sourceTaskId);

            _tasks[keySource] = sourceTaskId;
            _tasks[keyTarget] = targetTaskId;
        }

        private int GetKey(ITaskService service, string id)
        {
            int hash = 17;
            hash = hash * 31 + service.GetHashCode();
            hash = hash * 31 + id.GetHashCode();
            return hash;
        }

        #endregion Methods
    }
}
