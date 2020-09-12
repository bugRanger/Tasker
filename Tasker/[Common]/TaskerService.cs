namespace Tasker
{
    using System;

    using Services.Trello;
    using Services.GitLab;
    using Services.Redmine;

    public class TaskerService : ITaskerService
    {
        #region Fields

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
            Mapper = mapper;
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            _strategy.Start(this);
        }

        public void Stop()
        {
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

        public void SetStrategy(ITaskerStrategy strategy) 
        {
            _strategy = strategy;
        }

        #endregion Methods
    }
}
