namespace TrelloIntegration.Services.Redmine.Tasks
{
    using System;

    class SyncIssuesTask : Common.TaskItem<RedmineService, bool>
    {
        public IRedmineSync SyncOptions { get; }

        public SyncIssuesTask(IRedmineSync syncOptions, Action<bool> callback = null) : base(callback)
        {
            SyncOptions = syncOptions;
        }

        protected override bool HandleImpl(RedmineService service)
        {
            return service.Handle(this);
        }
    }
}
