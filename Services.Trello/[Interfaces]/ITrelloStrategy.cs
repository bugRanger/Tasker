namespace Services.Trello
{
    using System;

    public interface ITrelloStrategy
    {
        #region Methods

        void Register(ITrelloService visitor);

        void UpdateList(BoardList board);

        void UpdateComment(CardComment comment);

        #endregion Methods
    }
}
