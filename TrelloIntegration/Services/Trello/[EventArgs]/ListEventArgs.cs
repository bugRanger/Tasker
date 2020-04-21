namespace TrelloIntegration.Services.Trello
{
    class ListEventArgs : CardEventArgs
    {
        public string PrevListId { get; }

        public string CurrListId { get; }

        public ListEventArgs(string cardId, string prevId, string currId) : base(cardId)
        {
            PrevListId = prevId;
            CurrListId = currId;
        }
    }
}
