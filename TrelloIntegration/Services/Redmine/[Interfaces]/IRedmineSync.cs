namespace TrelloIntegration.Services
{
    interface IRedmineSync 
    {
        int Interval { get; }

        int AssignedId { get; }
    }
}
