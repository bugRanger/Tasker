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

        public string CardId => _getCardId?.Invoke() ?? null;

        public string ListId => _getListId?.Invoke() ?? null;

        public string Subject { get; }

        public string Description { get; }

        #endregion Properties

        public UpdateCardTask(Func<string> getCardId, Func<string> getListId, string subject, string description, Action<string> callback = null) : base(callback)
        {
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
