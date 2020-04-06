namespace Redmine2Trello.Services.Redmine.Tasks
{
    using System;

    class SyncIssuesTask : Common.TaskItem<RedmineService>
    {
        public IRedmineSync SyncOptions { get; }

        public SyncIssuesTask(IRedmineSync syncOptions, Action<bool> callback = null) : base(callback)
        {
            SyncOptions = syncOptions;
        }

        public override bool HandleImpl(RedmineService service)
        {
            return service.Handle(this);
        }
    }
}
