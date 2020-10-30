namespace Tasker.Common.Task
{
    using System;

    using Tasker.Interfaces.Task;

    public class TaskContext : ITaskContext
    {
        public string Status { get; set; }

        public string Name { get; set; }

        public string Desc { get; set; }
    }
}