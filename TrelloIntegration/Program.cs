namespace TrelloIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using TrelloIntegration.Services;
    using TrelloIntegration.Services.Trello.Tasks;
    using TrelloIntegration.Services.GitLab.Tasks;
    using TrelloIntegration.Services.Redmine.Tasks;
    using TrelloIntegration.Common;

    partial class Program
    {
        const string GITLAB_OPTIONS_FILE = "gitlabOptions.json";
        const string TRELLO_OPTIONS_FILE = "trelloOptions.json";
        const string REDMINE_OPTIONS_FILE = "redmineOptions.json";

        const int IN_PROGRESS_STATUS = 22;
        
        static void Main(string[] args)
        {
            var mapperStatus = new Dictionary<string, int>();
            var cardId2Issue = new Dictionary<string, IssueEntity>();

            TrelloOptions trelloOptions = JsonConfig.Read<TrelloOptions>(TRELLO_OPTIONS_FILE).Result;
            GitLabOptions gitlabOptions = JsonConfig.Read<GitLabOptions>(GITLAB_OPTIONS_FILE).Result;
            RedmineOptions redmineOptions = JsonConfig.Read<RedmineOptions>(REDMINE_OPTIONS_FILE).Result;

            try
            {
                using (var trello = new TrelloService(trelloOptions))
                using (var gitlab = new GitLabService(gitlabOptions))
                using (var redmine = new RedmineService(redmineOptions))
                {
                    redmine.UpdateStatuses += (s, statuses) =>
                    {
                        foreach (var status in statuses)
                        {
                            mapperStatus[status.Name] = status.Id;
                        }
                    };
                    redmine.UpdateIssues += (s, issues) =>
                    {
                        foreach (var issue in issues)
                        {
                            // TODO Add more detail for card issue.
                            trello.Enqueue(
                                new ImportCardTask(
                                    issue.Project.Name,
                                    $"[{issue.Id}] {issue.Subject}",
                                    issue.Description,
                                    issue.Status.Name,
                                cardId =>
                                {
                                    cardId2Issue[cardId] =
                                        new IssueEntity()
                                        {
                                            CardId = cardId,
                                            IssueId = issue.Id,
                                            Project = issue.Project.Name,
                                            Subject = $"[{issue.Id}] {issue.Subject}",
                                            Discription = issue.Description,
                                            Status = issue.Status.Name,
                                        };
                                }));
                        }
                    };

                    trello.CreateBoard += (s, args) =>
                    {
                        // TODO Add filter for status.
                        trello.Enqueue(new UpdateListTask(args.BroadId, mapperStatus.Keys.ToArray()));
                    };
                    trello.UpdateStatus += (s, args) =>
                    {
                        if (!cardId2Issue.ContainsKey(args.CardId) ||
                            !mapperStatus.ContainsKey(args.StatusNew))
                            return;

                        if (cardId2Issue[args.CardId].Status != args.StatusNew)
                            redmine.Enqueue(new UpdateIssueTask(cardId2Issue[args.CardId].IssueId, mapperStatus[args.StatusNew],
                                result =>
                                {
                                    if (!result)
                                    {
                                    // Return old status.
                                    trello.Enqueue(
                                            new ImportCardTask(
                                                cardId2Issue[args.CardId].Project,
                                                cardId2Issue[args.CardId].Subject,
                                                cardId2Issue[args.CardId].Discription,
                                                cardId2Issue[args.CardId].Status));
                                        return;
                                    }

                                // TODO Add script for redmine actions on change status.
                                if (mapperStatus[args.StatusOld] == IN_PROGRESS_STATUS)
                                        redmine.Enqueue(new UpdateWorkTimeTask(cardId2Issue[args.CardId].IssueId));
                                }));
                    };

                    gitlab.Start();
                    redmine.Start();
                    trello.Start();

                    while (true)
                    {
                        var keyInfo = Console.ReadKey();
                        if (keyInfo.Key == ConsoleKey.Q)
                            break;
                    }

                    trello.Stop();
                    gitlab.Stop();
                    redmine.Stop();
                }
            }
            finally
            {
                JsonConfig.Write(trelloOptions, TRELLO_OPTIONS_FILE).Wait();
                JsonConfig.Write(gitlabOptions, GITLAB_OPTIONS_FILE).Wait();
                JsonConfig.Write(redmineOptions, REDMINE_OPTIONS_FILE).Wait();
            }
        }
    }
}
