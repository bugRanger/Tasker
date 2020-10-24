namespace Tasker
{
    using System;

    using Framework.Common;

    using Services.Trello;
    using Services.GitLab;
    using Services.Redmine;

    public class TaskerService : ITaskerService
    {
        #region Fields

        private readonly Locker _locker;

        private ITaskerStrategy _strategy;

        #endregion Fields

        #region Properties

        public ITrelloService TrelloService { get; private set; }

        public IGitLabService GitLabService { get; private set; }

        public IRedmineService RedmineService { get; private set; }

        public IServiceMapper Mapper { get; }

        #endregion Properties

        #region Constructors

        public TaskerService(IServiceMapper mapper)
        {
            _locker = new Locker();

            Mapper = mapper;
        }

        #endregion Constructors

        #region Methods

        public void Start(ITaskerStrategy strategy = null)
        {
            if (!_locker.SetEnabled())
                return;

            _strategy ??= strategy;
            _strategy.Start(this);
        }

        public void Stop()
        {
            if (!_locker.SetDisabled())
                return;

            _strategy.Stop();
        }

        public void Register(ITrelloService service) 
        {
            TrelloService = service;
        }

        public void Register(IGitLabService service)
        {
            GitLabService = service;
        }

        public void Register(IRedmineService service)
        {
            RedmineService = service;
        }

        #endregion Methods
    }
}
