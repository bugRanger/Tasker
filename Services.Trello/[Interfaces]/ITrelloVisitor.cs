namespace Services.Trello
{
    using Common.Tasks;

    using Services.Trello.Tasks;

    public interface ITrelloVisitor : ITaskVisitor
    {
        #region Methods

        string Handle(UpdateCardTask task);

        #endregion Methods
    }
}
