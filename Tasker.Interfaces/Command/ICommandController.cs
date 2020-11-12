namespace Tasker.Interfaces.Command
{
    using System;

    public interface ICommandController
    {
        #region Methods

        ICommandController Register<TCommand, TArgs>(string uid, Action<TCommand, TArgs> execute)
            where TCommand : ICommandItem, new()
            where TArgs : ICommandArgs;

        bool TryParse(string text, out ICommandItem command);

        bool TryAction(ICommandItem command, ICommandArgs args);

        #endregion Methods
    }
}
