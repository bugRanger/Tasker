namespace TrelloIntegration.Services
{
    interface IGitLabSync 
    {
        int Interval { get; }

        int UserId { get; }
    }
}
