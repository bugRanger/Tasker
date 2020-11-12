namespace Tasker.Common.Command
{
    using System.Text.RegularExpressions;

    using Tasker.Interfaces.Command;

    public abstract class CommandItem : ICommandItem
    {
        #region Properties

        public string Expression { get; }

        #endregion Properties

        #region Constructors

        protected CommandItem(string expression)
        {
            Expression = expression;
        }

        #endregion Constructors

        #region Methods

        public abstract bool Reload(MatchCollection matches);

        #endregion Methods
    }
}
