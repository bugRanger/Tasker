namespace Services.GitLab
{
    using Common.Tasks;

    public interface IGitLabService
    {
        #region Properties

        IGitLabOptions Options { get; }

        #endregion Properties

        #region Methods

        void Start();

        void Stop();

        void Enqueue(ITaskItem<IGitLabVisitor> task);

        void Register(IGitLabBehaviors behaviors);

        #endregion Methods
    }
}
