namespace TrelloIntegration.Services
{
    class TrelloOptions : ITrelloOptions, ITrelloSync
    {
        public string AppKey { get; set; }

        public string Token { get; set; }
        
        ITrelloSync ITrelloOptions.Sync => this;

        public int Interval { get; set; }
    }
}
