namespace Services.Trello.Tasks
{
    using System;

    using Common.Tasks;

    public class UpdateCardFieldTask : TaskItem<ITrelloService, bool>, IUpdateCardFieldTask
    {
        public string FieldId { get; }

        public string CardId { get; }

        // TODO: Постараться уйти от объекта, разделив задачи по типу поля. 
        public object Value { get; }

        public UpdateCardFieldTask(string fieldId, string cardId, object value, Action<bool> callback = null) : base(callback)
        {
            FieldId = fieldId;
            CardId = cardId;
            Value = value;
        }

        protected override bool HandleImpl(ITrelloService service)
        {
            return service.Handle(this);
        }
    }
}
