namespace Redmine2Trello.Services.Redmine.Tasks
{
    class SyncStatusesTask : Common.TaskItem<RedmineService>
    {
        public override void Handle(RedmineService service)
        {
            service.Handle(this);
        }
    }
}
