namespace Tasker
{
    using Services.GitLab;
    using Services.Redmine;
    using Services.Trello;

    public interface ISettingServices
    {
        ITrelloOptions TrelloOptions { get; }

        IGitLabOptions GitLabOptions { get; }

        IRedmineOptions RedmineOptions { get; }
    }
}