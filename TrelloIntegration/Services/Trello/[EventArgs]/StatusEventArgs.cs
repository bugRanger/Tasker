namespace TrelloIntegration.Services.Trello
{
    class StatusEventArgs : CardEventArgs
    {
        public string PrevStatus { get; }

        public string CurrentStatus { get; }

        public StatusEventArgs(string cardId, string statusOld, string statusNew) : base(cardId)
        {
            PrevStatus = statusOld;
            CurrentStatus = statusNew;
        }
    }
}
