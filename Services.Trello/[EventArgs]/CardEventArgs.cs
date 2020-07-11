namespace Services.Trello
{
    using System;

    public abstract class CardEventArgs : EventArgs
    {
        public string CardId { get; }

        protected CardEventArgs(string cardId) 
        {
            CardId = cardId;
        }
    }
}
