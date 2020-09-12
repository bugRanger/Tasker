namespace Services.Trello
{
    using System;

    public interface ITrelloBehaviors
    {
        #region Methods

        void UpdateList(BoardList board);

        void UpdateComment(CardComment comment);

        #endregion Methods
    }
}
