namespace TrelloIntegration.Services.Redmine.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class SyncStatusesTask : TaskItem<RedmineService, bool>
    {
        public SyncStatusesTask(Action<bool> callback = null) : base(callback)
        {
        }

        protected override bool HandleImpl(RedmineService service)
        {
            return service.Handle(this);
        }
    }
}
