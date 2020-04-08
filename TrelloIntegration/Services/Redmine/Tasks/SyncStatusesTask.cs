using System;

namespace TrelloIntegration.Services.Redmine.Tasks
{
    class SyncStatusesTask : Common.TaskItem<RedmineService, bool>
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
