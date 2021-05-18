namespace Tasker.Common.Task
{
    using System;
    using System.Collections.Generic;

    using Tasker.Interfaces.Task;

    public class TaskContext : ITaskContext, IEquatable<ITaskContext>
    {
        #region Fields

        private static readonly Dictionary<string, Action<ITaskContext, TaskContext>> _propertySetter;

        #endregion Fields

        #region Properties

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public TaskState Status { get; set; }

        public TaskKind Kind { get; set; }

        public MergeState MergeStatus { get; set; }

        #endregion Properties

        #region Methods

        static TaskContext()
        {
            _propertySetter = new Dictionary<string, Action<ITaskContext, TaskContext>>
            {
                [nameof(ITaskContext.Id)] = (source, dest) => dest.Id = source.Id,
                [nameof(ITaskContext.Kind)] = (source, dest) => dest.Kind = source.Kind,
                [nameof(ITaskContext.Name)] = (source, dest) => dest.Name = source.Name,
                [nameof(ITaskContext.Status)] = (source, dest) => dest.Status = source.Status,
                [nameof(ITaskContext.Description)] = (source, dest) => dest.Description = source.Description,
                [nameof(ITaskContext.MergeStatus)] = (source, dest) => dest.MergeStatus = source.MergeStatus,
            };
        }

        public void ApplyPatch(ITaskContext source, IEnumerable<string> properties)
        {
            foreach (var property in properties)
            {
                if (!_propertySetter.TryGetValue(property, out var setter))
                    continue;

                setter.Invoke(source, this);
            }
        }

        public bool Equals(ITaskContext other)
        {
            return other != null
                && Id == other.Id
                && Name == other.Name
                && Description == other.Description
                && Kind == other.Kind
                && Status == other.Status;
        }

        #endregion Methods
    }
}
