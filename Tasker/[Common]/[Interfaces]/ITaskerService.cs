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
    }
}
