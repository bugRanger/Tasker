namespace TrelloIntegration.Services.Trello.Commands
{
    using System.Text.RegularExpressions;

    using TrelloIntegration.Common.Command;

    class MergeCommand : CommandItem
    {
        #region Fields

        private static string EXPRESSION = "([A-Za-z0-9]+) ([A-Za-z]+) ([A-Za-z]+)$";

        #endregion Fields
        
        #region Properties

        public string Source { get; private set; }

        public string Target { get; private set; }

        public string Title { get; private set; }

        #endregion Properties

        public MergeCommand() 
            : base(EXPRESSION)
        {
        }

        #region Methods

        internal override bool Reload(MatchCollection matches)
        {
            Source = matches[0].Groups[1].Value;
            Target = matches[0].Groups[2].Value;
            Title = matches[0].Groups[3].Value;

            return true;
        }

        #endregion Methods
    }
}
