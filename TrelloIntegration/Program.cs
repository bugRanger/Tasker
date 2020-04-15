namespace TrelloIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using TrelloIntegration.Common;

    using TrelloIntegration.Services;
    using TrelloIntegration.Services.Trello;
    using TrelloIntegration.Services.Trello.Tasks;

    using TrelloIntegration.Services.GitLab;
    using TrelloIntegration.Services.GitLab.Tasks;

    using TrelloIntegration.Services.Redmine;
    using TrelloIntegration.Services.Redmine.Tasks;

    using Manatee.Trello;

    partial class Program
    {
        const string GITLAB_OPTIONS_FILE = "gitlabOptions.json";
        const string TRELLO_OPTIONS_FILE = "trelloOptions.json";
        const string REDMINE_OPTIONS_FILE = "redmineOptions.json";

        const string TRELLO_CMD_UPDATE_TIME = "^uptime: (([0-9]+[\\.\\,])?[0-9]+) - (.*$)";

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
                    redmine.Error += (s, error) => Console.WriteLine(error);
                    redmine.UpdateStatuses += (s, statuses) =>
                    {
                        if (redmineOptions.Statuses == null)
                            return;

                        var dict = statuses.ToDictionary(k => k.Id, v => v);

                        foreach (int statusId in redmineOptions.Statuses)
                        {
                            if (dict.TryGetValue(statusId, out var issueStatus))
                                mapperStatus[issueStatus.Name] = issueStatus.Id;
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
                                    mapperStatus.Keys.ToArray(),
                                    cardId =>
                                    {
                                        if (string.IsNullOrWhiteSpace(cardId))
                                            return;

                                        cardId2Issue[cardId] =
                                            new IssueEntity()
                                            {
                                                CardId = cardId,
                                                IssueId = issue.Id,
                                                Project = issue.Project.Name,
                                                Subject = $"[{issue.Id}] {issue.Subject}",
                                                Discription = issue.Description,
                                                Status = issue.Status.Name,
                                                UpdateDT = issue.UpdatedOn ?? issue.CreatedOn,
                                            };
                                    }));
                        }
                    };

                    trello.Error += (s, error) => Console.WriteLine(error);
                    trello.UpdateComments += (s, args) =>
                    {
                        if (!cardId2Issue.ContainsKey(args.CardId) ||
                            args.UserId != trello.UserId)
                            return;

                        var matches = Regex.Matches(args.Text.ToLower(), TRELLO_CMD_UPDATE_TIME);
                        if (matches.Count > 0 &&
                            matches[0].Success &&
                            decimal.TryParse(matches[0].Groups[1].Value.Replace('.', ','), out decimal hours))
                            redmine.Enqueue(
                                new UpdateWorkTimeTask(
                                    cardId2Issue[args.CardId].IssueId,
                                    hours,
                                    matches[0].Groups[3].Value,
                                    result =>
                                    {
                                        trello.Enqueue(new EmojiCommentTask(args.CardId, args.CommentId,
                                            result ? Emojis.WhiteCheckMark : Emojis.FaceWithSymbolsOnMouth));
                                    }));
                    };
                    trello.UpdateStatus += (s, args) =>
                    {
                        if (!cardId2Issue.ContainsKey(args.CardId) ||
                            !mapperStatus.ContainsKey(args.CurrentStatus))
                            return;

                        var hours = Convert.ToDecimal((DateTime.Now - cardId2Issue[args.CardId].UpdateDT).Value.TotalHours);

                        if (cardId2Issue[args.CardId].Status != args.CurrentStatus)
                            redmine.Enqueue(new UpdateIssueTask(cardId2Issue[args.CardId].IssueId, mapperStatus[args.CurrentStatus]));
                    };

                    gitlab.Error += (s, error) => Console.WriteLine(error);

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
