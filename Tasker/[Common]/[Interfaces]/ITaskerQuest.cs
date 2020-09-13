namespace Tasker
{
    using System;

    public interface ITaskerQuest
    {
        IUid<string> Id { get; }

        IUid<string> Status { get; }
    }
}
