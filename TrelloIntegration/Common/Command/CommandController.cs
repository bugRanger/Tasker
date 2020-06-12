namespace TrelloIntegration.Common.Command
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class CommandController : ICommandController
    {
        #region Fields

        private Func<string> _getformat;
        private Dictionary<string, Func<ICommandItem>> _createMapper;
        private Dictionary<Type, Action<ICommandItem, ICommandArgs>> _actionMapper;

        #endregion Fields

        #region Constructors

        public CommandController(Func<string> getformat)
        {
            _getformat = getformat;

            _createMapper = new Dictionary<string, Func<ICommandItem>>();
            _actionMapper = new Dictionary<Type, Action<ICommandItem, ICommandArgs>>();
        }

        #endregion Constructors

        public CommandController Register<TCommand, TArgs>(string uid, Action<TCommand, TArgs> execute)
            where TCommand : ICommandItem, new()
            where TArgs : ICommandArgs
        {
            _createMapper[uid] = () => new TCommand();
            _actionMapper[typeof(TCommand)] = (command, args) => execute?.Invoke((TCommand)command, (TArgs)args);

            return this;
        }

        public bool TryParse(string text, out ICommandItem command)
        {
            command = null;

            var match = Regex.Match(text, _getformat?.Invoke() ?? string.Empty);
            if (!match.Success || !_createMapper.TryGetValue(match.Groups[1].Value, out var createCommand))
                return false;

            command = createCommand();

            var matches = Regex.Matches(text, command.Expression);
            if (matches.Count == 0 || !command.Reload(matches))
                return false;

            return true;
        }

        public bool TryAction(ICommandItem command, ICommandArgs args)
        {
            if (command == null || !_actionMapper.TryGetValue(command.GetType(), out var actionCommand))
                return false;

            actionCommand(command, args);
            return true;
        }
    }
}
