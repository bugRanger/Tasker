﻿namespace TrelloIntegration.Common.Command
{
    using System;

    interface ICommandController
    {
        #region Methods

        CommandController Register<TCommand, TArgs>(string uid, Action<TCommand, TArgs> execute)
            where TCommand : ICommandItem, new()
            where TArgs : ICommandArgs;

        bool TryParse(string text, out ICommandItem command);

        bool TryAction(ICommandItem command, ICommandArgs args);

        #endregion Methods
    }
}
