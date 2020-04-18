namespace TrelloIntegration.Services.Trello.Commands
{
    using System.Text.RegularExpressions;

    using TrelloIntegration.Common.Command;

    class UptimeCommand : CommandItem
    {
        #region Fields

        public static string UID = "uptime";

        private static string EXPRESSION = "(([0-9]+[\\.\\,])?[0-9]+) (.*)$";

        #endregion Fields
        
        #region Properties

        public decimal Hours { get; private set; }
        
        public string Comment { get; private set; }

        #endregion Properties

        public UptimeCommand() 
            : base(UID, EXPRESSION)
        {
        }

        #region Methods

        internal override bool Reload(MatchCollection matches)
        {
            if (!decimal.TryParse(matches[0].Groups[1].Value.Replace('.', ','), out decimal hours))
                return false;

            Hours = hours;
            Comment = matches[0].Groups[3].Value;

            return true;
        }

        #endregion Methods
    }
}
