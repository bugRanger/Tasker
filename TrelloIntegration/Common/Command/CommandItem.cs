namespace TrelloIntegration.Common.Command
{
    using System.Text.RegularExpressions;

    public abstract class CommandItem
    {
        #region Properties

        public string Expression { get; }

        #endregion Properties

        protected CommandItem(string expression)
        {
            Expression = expression;
        }

        #region Methods

        internal abstract bool Reload(MatchCollection matches);

        #endregion Methods
    }
}
