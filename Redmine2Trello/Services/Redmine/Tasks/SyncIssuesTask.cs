namespace Redmine2Trello.Services.Redmine.Tasks
{
    class SyncIssuesTask : Common.TaskItem<RedmineService>
    {
        public IRedmineSync SyncOptions { get; }

        public SyncIssuesTask(IRedmineSync syncOptions) 
        {
            SyncOptions = syncOptions;
        }

        public override void Handle(RedmineService service)
        {
            service.Handle(this);
        }
    }
}
