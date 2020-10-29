namespace Tasker.Common
{
    using Tasker.Interfaces;

    public class TaskContext : ITaskContext
    {
        public string Status { get; set; }

        public string Name { get; set; }

        public string Desc { get; set; }
    }
}