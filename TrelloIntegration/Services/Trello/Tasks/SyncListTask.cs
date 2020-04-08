namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;

    class SyncListTask : Common.TaskItem<TrelloService, bool>
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
