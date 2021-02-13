namespace Tasker.Common.Task
{
    using System;
    using System.Collections.Generic;

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

        public void ApplyPatch(ITaskContext source, IEnumerable<string> properties)
        {
            foreach (var property in properties)
            {
                switch (property)
                {
                    case nameof(ITaskContext.Id):
                        Id = source.Id;
                        break;

                    case nameof(ITaskContext.Name):
                        Name = source.Name;
                        break;

                    case nameof(ITaskContext.Description):
                        Description = source.Description;
                        break;

                    case nameof(ITaskContext.Kind):
                        Kind = source.Kind;
                        break;

                    case nameof(ITaskContext.MergeStatus):
                        MergeStatus = source.MergeStatus;
                        break;

                    case nameof(ITaskContext.Status):
                        Status = source.Status;
                        break;

                    default:
                        break;
                }
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
