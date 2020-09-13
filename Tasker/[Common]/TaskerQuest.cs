namespace Tasker
{
    using System;

    public class TaskerQuest : ITaskerQuest
    {
        public IUid<string> Id { get; }

        public IUid<string> Status { get; }

        public TaskerQuest() 
        {
            Id = new Uid<string>();
            Status = new Uid<string>();
        }
    }
}
