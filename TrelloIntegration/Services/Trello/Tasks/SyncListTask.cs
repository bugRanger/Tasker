namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class SyncListTask : TaskItem<TrelloService, bool>
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
