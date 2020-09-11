namespace Services.Trello
{
    using Common.Tasks;

    using Services.Trello.Tasks;

    public interface ITrelloVisitor : ITaskVisitor
    {
        #region Methods

        string Handle(IUpdateBoardTask task);

        string Handle(IUpdateFieldTask task);

        string Handle(IUpdateLabelTask task);

        string Handle(IUpdateListTask task);

        string Handle(IUpdateCardTask task);

        bool Handle(IUpdateCardFieldTask task);

        bool Handle(IAddCommentTask task);

        bool Handle(IEmojiCommentTask task);

        #endregion Methods
    }
}
