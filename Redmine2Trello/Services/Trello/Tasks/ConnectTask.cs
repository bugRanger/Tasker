namespace Redmine2Trello.Services.Trello.Tasks
{
    class ConnectTask : Common.TaskItem<TrelloService>
    {
        public override void Handle(TrelloService service)
        {
            service.Handle(this);
        }
    }
}
