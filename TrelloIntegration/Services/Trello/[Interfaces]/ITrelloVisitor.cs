namespace TrelloIntegration.Services.Trello
{
    using System;

    using TrelloIntegration.Common.Tasks;
    using TrelloIntegration.Services.Trello.Tasks;

    interface ITrelloVisitor : IServiceVisitor
    {
        #region Methods

        string Handle(UpdateBoardTask task);

        string Handle(UpdateFieldTask task);

        string Handle(UpdateLabelTask task);

        string Handle(UpdateListTask task);

        string Handle(UpdateCardTask task);

        bool Handle(UpdateCardFieldTask task);

        bool Handle(AddCommentTask task);

        bool Handle(EmojiCommentTask task);

        #endregion Methods
    }
}
