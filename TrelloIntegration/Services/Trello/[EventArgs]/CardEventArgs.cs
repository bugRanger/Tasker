namespace TrelloIntegration.Services
{
    class CardEventArgs 
    {
        public string CardId { get; }

        public string StatusOld { get; }

        public string StatusNew { get; }

        public CardEventArgs(string cardId, string statusOld, string statusNew) 
        {
            CardId = cardId;
            StatusOld = statusOld;
            StatusNew = statusNew;
        }
    }
}
