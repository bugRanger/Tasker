namespace Services.Redmine
{
    using Common.Tasks;

    public interface IRedmineService
    {
        #region Properties

        IRedmineOptions Options { get; }

        #endregion Properties

        #region Methods

        void Start();

        void Stop();

        void Enqueue(ITaskItem<IRedmineVisitor> task);

        void Register(IRedmineBehaviors behaviors);

        #endregion Methods
    }
}
