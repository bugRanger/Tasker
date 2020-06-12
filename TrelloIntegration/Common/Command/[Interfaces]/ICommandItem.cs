namespace TrelloIntegration.Common.Command
{
    using System.Text.RegularExpressions;

    public interface ICommandItem
    {
        #region Properties

        string Expression { get; }

        #endregion Properties

        #region Methods

        bool Reload(MatchCollection matches);

        #endregion Methods
    }
}
