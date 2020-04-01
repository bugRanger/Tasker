namespace Redmine2Trello.Services.Trello.Tasks
{
    class SyncListTask : Common.TaskItem<TrelloService>
    {
        public ITrelloSync SyncOptions { get; }

        public SyncListTask(ITrelloSync syncOptions)
        {
            SyncOptions = syncOptions;
        }

        public override void Handle(TrelloService service) 
        {
            service.Handle(this);
        }
    }
}
