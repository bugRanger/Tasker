namespace TrelloIntegration.Services
{
    class BroadEventArgs 
    {
        public string BroadId { get; }

        public BroadEventArgs(string broadId)
        {
            BroadId = broadId;
        }
    }
}
