namespace Redmine2Trello.Services
{
    interface IRedmineSync 
    {
        int Interval { get; }

        int AssignedId { get; }

        int[] StatusIds { get; }
    }
}
