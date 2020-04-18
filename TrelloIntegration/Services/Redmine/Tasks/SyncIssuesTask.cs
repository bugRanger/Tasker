namespace TrelloIntegration.Services.Redmine.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class SyncIssuesTask : TaskItem<RedmineService, bool>
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
