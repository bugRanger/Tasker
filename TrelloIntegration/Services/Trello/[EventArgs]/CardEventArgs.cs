namespace TrelloIntegration.Services.Trello
{
    abstract class CardEventArgs
    {
        public string CardId { get; }

        protected CardEventArgs(string cardId) 
        {
            CardId = cardId;
        }
    }
}
