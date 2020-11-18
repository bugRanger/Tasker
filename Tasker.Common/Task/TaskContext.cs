namespace Tasker.Common.Task
{
    using System;

    using Tasker.Interfaces.Task;

    public class TaskContext : ITaskContext, IEquatable<ITaskContext>
    {
        #region Properties

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public TaskState Status { get; set; }

        public TaskKind Kind { get; set; }

        public MergeState MergeStatus { get; set; }

        #endregion Properties

        #region Methods

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
