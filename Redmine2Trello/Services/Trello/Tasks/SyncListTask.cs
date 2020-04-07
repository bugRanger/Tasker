namespace Redmine2Trello.Services.Trello.Tasks
{
    using System;

    class SyncListTask : Common.TaskItem<TrelloService>
    {
        public ITrelloSync SyncOptions { get; }

        public SyncListTask(ITrelloSync syncOptions, Action<bool> callback = null) : base(callback)
        {
            SyncOptions = syncOptions;
        }

        protected override bool HandleImpl(TrelloService service) 
        {
            return service.Handle(this);
        }
    }
}
