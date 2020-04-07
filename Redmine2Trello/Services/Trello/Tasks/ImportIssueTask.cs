namespace Redmine2Trello.Services.Trello.Tasks
{
    using System;
    
    class ImportIssueTask : Common.TaskItem<TrelloService>
    {
        public IssueCard IssueCard { get; }

        public ImportIssueTask(IssueCard issueCard, Action<bool> callback = null) : base(callback)
        {
            IssueCard = issueCard;
        }

        protected override bool HandleImpl(TrelloService service)
        {
            return service.Handle(this);
        }
    }
}
