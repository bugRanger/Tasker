namespace TrelloIntegration.Common.Command
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class CommandController
    {
        #region Fields

        private Func<string> _getformat;
        private Dictionary<string, Func<CommandItem>> _createMapper;
        private Dictionary<Type, Action<CommandItem, EventArgs>> _actionMapper;

        #endregion Fields

        #region Constructors

        public CommandController(Func<string> getformat)
        {
            _getformat = getformat;
            _createMapper = new Dictionary<string, Func<CommandItem>>();
            _actionMapper = new Dictionary<Type, Action<CommandItem, EventArgs>>();
        }

        #endregion Constructors

        public CommandController Register<TCommand, TArgs>(string uid, Action<TCommand, TArgs> execute)
            where TCommand : CommandItem, new()
            where TArgs : EventArgs
        {
            _createMapper[uid] = () => new TCommand();
            _actionMapper[typeof(TCommand)] = (command, args) => execute?.Invoke((TCommand)command, (TArgs)args);

            return this;
        }

        public bool TryAction(string command, EventArgs args)
        {
            var match = Regex.Match(command, _getformat?.Invoke() ?? string.Empty);
            if (!match.Success || !_createMapper.TryGetValue(match.Groups[1].Value, out var createCommand))
                return false;

            var commandItem = createCommand();

            var matches = Regex.Matches(command, commandItem.Expression);
            if (matches.Count == 0 || !commandItem.Reload(matches))
                return false;

            if (!_actionMapper.TryGetValue(commandItem.GetType(), out var actionCommand))
                return false;

            actionCommand(commandItem, args);

            return true;
        }
    }
}
