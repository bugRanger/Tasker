namespace TrelloIntegration.Common.Command
{
    using System;
    using System.Text.RegularExpressions;

    public abstract class CommandItem
    {
        #region Properties

        public string Uid { get; }

        public string Expression { get; }

        #endregion Properties

        protected CommandItem(string uid, string expression)
        {
            Uid = uid;
            Expression = expression;
        }

        #region Methods

        internal abstract bool Reload(MatchCollection matches);

        #endregion Methods
    }
}
