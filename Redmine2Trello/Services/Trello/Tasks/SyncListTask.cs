namespace Redmine2Trello.Services.Trello.Tasks
{
    class SyncListTask : Common.TaskItem<TrelloService>
    {
        public string ListId { get; set; }

        public override void Handle(TrelloService service) 
        {
            service.Handle(this);
        }
    }
}
