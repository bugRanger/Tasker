namespace Tasker
{
    using System;

    using Services.GitLab;
    using Services.Redmine;
    using Services.Trello;

    public interface ITaskerService
    {
        #region Properties

        ITrelloService TrelloService { get; }

        IGitLabService GitLabService { get; }

        IRedmineService RedmineService { get; }

        IServiceMapper Mapper { get; }

        #endregion Properties

        #region Methods

        void Start(ITaskerStrategy strategy = null);

        void Stop();

        void Register(ITrelloService service);

        void Register(IGitLabService service);

        void Register(IRedmineService service);

        #endregion Methods
    }
}
