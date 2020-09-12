namespace Services.Trello
{
    using Common.Tasks;

    public interface ITrelloService
    {
        #region Properties

        // TODO Move to optioins.
        string Mention { get; }

        ITrelloOptions Options { get; }

        #endregion Properties

        #region Methods

        void Start();

        void Stop();

        void Enqueue(ITaskItem<ITrelloVisitor> task);

        void Register(ITrelloBehaviors behaviors);

        #endregion Methods
    }
}
