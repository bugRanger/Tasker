namespace Redmine2Trello.Services.Trello.Tasks
{
    class UpdateListTask : Common.TaskItem<TrelloService>
    {
        public string BoardId { get; }

        public string[] Lists { get; }

        public UpdateListTask(string boardId, string[] lists)
        {
            BoardId = boardId;
            Lists = lists;
        }

        public override void Handle(TrelloService service) 
        {
            service.Handle(this);
        }
    }
}
