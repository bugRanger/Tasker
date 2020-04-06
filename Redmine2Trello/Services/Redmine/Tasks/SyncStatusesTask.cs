using System;

namespace Redmine2Trello.Services.Redmine.Tasks
{
    class SyncStatusesTask : Common.TaskItem<RedmineService>
    {
        public SyncStatusesTask(Action<bool> callback = null) : base(callback)
        {
        }

        public override bool HandleImpl(RedmineService service)
        {
            return service.Handle(this);
        }
    }
}
