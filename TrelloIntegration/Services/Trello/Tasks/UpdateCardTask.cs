namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateCardTask : TaskItem<TrelloService, string>
    {
        #region Fields

        // Lazy.
        private Func<string> _getCardId;
        private Func<string> _getListId;

        #endregion Fields

        #region Properties
        public string BoardId { get; }

        public string CardId => _getCardId?.Invoke() ?? null;

        public string ListId => _getListId?.Invoke() ?? null;

        public string Subject { get; }

        public string Description { get; }

        #endregion Properties

        public UpdateCardTask(string boardId, Func<string> getCardId, Func<string> getListId, string subject = null, string description = null, Action<string> callback = null) : base(callback)
        {
            BoardId = boardId;
            _getCardId = getCardId;
            _getListId = getListId;

            Subject = subject;
            Description = description;
        }

        protected override string HandleImpl(TrelloService service)
        {
            return service.Handle(this);
        }
    }
}
