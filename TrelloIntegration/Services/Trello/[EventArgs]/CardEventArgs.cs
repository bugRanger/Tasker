namespace TrelloIntegration.Services.Trello
{
    using System;

    abstract class CardEventArgs : EventArgs
    {
        public string CardId { get; }

        protected CardEventArgs(string cardId) 
        {
            CardId = cardId;
        }
    }
}
