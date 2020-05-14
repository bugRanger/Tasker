namespace TrelloIntegration.Services.Trello.Tasks
{
    using System;

    using TrelloIntegration.Common.Tasks;

    class UpdateCardFieldTask : TaskItem<ITrelloVisitor, bool>
    {
        public string FieldId { get; }

        public string CardId { get; }

        public object Value { get; }

        public UpdateCardFieldTask(string fieldId, string cardId, object value, Action<bool> callback = null) : base(callback)
        {
            FieldId = fieldId;
            CardId = cardId;
            Value = value;
        }

        protected override bool HandleImpl(ITrelloVisitor service)
        {
            return service.Handle(this);
        }
    }
}
