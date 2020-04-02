namespace Redmine2Trello.Services
{
    using System.Collections.Generic;

    interface ITrelloSync
    {
        IList<string> BoardIds { get; }

        int Interval { get; }
    }
}
