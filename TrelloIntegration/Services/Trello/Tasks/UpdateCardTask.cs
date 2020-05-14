namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;
    using TrelloIntegration.Common.Tasks;

    class UpdateCardTask : TaskItem<ITrelloVisitor, string>
    {
        #region Fields

        // Lazy.
        private Func<string> _getCardId;
        private Func<string> _getListId;
        private Func<string> _getLabelId;

        #endregion Fields

        #region Properties
        public string BoardId { get; }

        public string CardId => _getCardId?.Invoke() ?? null;

        public string ListId => _getListId?.Invoke() ?? null;

        public string LabelId => _getLabelId?.Invoke() ?? null;

        public string Subject { get; }

        public string Description { get; }

        #endregion Properties

        public UpdateCardTask(string boardId, Func<string> getCardId, Func<string> getListId, Func<string> getLabelId = null, string subject = null, 
            string description = null, Action<string> callback = null) : base(callback)
        {
            BoardId = boardId;
            _getCardId = getCardId;
            _getListId = getListId;
            _getLabelId = getLabelId;

            Subject = subject;
            Description = description;
        }

        protected override string HandleImpl(ITrelloVisitor service)
        {
            return service.Handle(this);
        }
    }
}
