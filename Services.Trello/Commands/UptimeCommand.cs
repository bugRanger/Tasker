namespace Services.Trello.Commands
{
    using System.Text.RegularExpressions;

    using Common.Command;

    public class UptimeCommand : CommandItem
    {
        #region Fields

        private static string EXPRESSION = "(([0-9]+[\\.\\,])?[0-9]+) (.*)$";

        #endregion Fields
        
        #region Properties

        public decimal Hours { get; private set; }
        
        public string Comment { get; private set; }

        #endregion Properties

        public UptimeCommand() 
            : base(EXPRESSION)
        {
        }

        #region Methods

        public override bool Reload(MatchCollection matches)
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
