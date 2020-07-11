namespace Services.Trello
{
    public class BroadEventArgs 
    {
        public string BroadId { get; }

        public BroadEventArgs(string broadId)
        {
            BroadId = broadId;
        }
    }
}
