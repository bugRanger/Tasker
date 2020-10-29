namespace Services.Trello
{
    public class TrelloOptions : ITrelloOptions, ITrelloSync
    {
        #region Properties

        public string Mention { get; set; }

        public string AppKey { get; set; }

        public string Token { get; set; }

        public string BoardId { get; set; }

        public string BoardName { get; set; }

        ITrelloSync ITrelloOptions.Sync => this;

        public int Interval { get; set; }

        #endregion Properties
    }
}
