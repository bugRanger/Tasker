namespace TrelloIntegration
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using TrelloIntegration.Common;
    using TrelloIntegration.Common.Command;

    using TrelloIntegration.Services;
    using TrelloIntegration.Services.Trello;
    using TrelloIntegration.Services.Trello.Tasks;
    using TrelloIntegration.Services.Trello.Commands;

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

        private static TrelloService trelloService;
        private static GitLabService gitlabService;
        private static RedmineService redmineService;

        private static CommandController trelloCommand;

        private static Dictionary<string, int> mapperStatus;
        private static Dictionary<string, IssueEntity> cardId2Issue;

        static void Main(string[] args)
        {
            mapperStatus = new Dictionary<string, int>();
            cardId2Issue = new Dictionary<string, IssueEntity>();

            TrelloOptions trelloOptions = JsonConfig.Read<TrelloOptions>(TRELLO_OPTIONS_FILE).Result;
            GitLabOptions gitlabOptions = JsonConfig.Read<GitLabOptions>(GITLAB_OPTIONS_FILE).Result;
            RedmineOptions redmineOptions = JsonConfig.Read<RedmineOptions>(REDMINE_OPTIONS_FILE).Result;
                        
            try
            {
                trelloCommand = new CommandController(() => $"^{trelloService.Mention} ([A-Za-z]+):");
                trelloCommand.Register<UptimeCommand, CommentEventArgs>(UptimeCommand.UID, UptimeCommandAction);

                using (trelloService = new TrelloService(trelloOptions))
                using (gitlabService = new GitLabService(gitlabOptions))
                using (redmineService = new RedmineService(redmineOptions))
                {
                    redmineService.Error += (s, error) => Console.WriteLine(error);
                    redmineService.UpdateStatuses += (s, statuses) =>
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
                    redmineService.UpdateIssues += (s, issues) =>
                    {
                        foreach (var issue in issues)
                        {
                            // TODO Add more detail for card issue.
                            trelloService.Enqueue(
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

                    trelloService.Error += (s, error) => Console.WriteLine(error);
                    trelloService.UpdateComments += (s, args) =>
                    {
                        if (!cardId2Issue.ContainsKey(args.CardId))
                            return;

                        trelloCommand.TryAction(args.Text, args);
                    };
                    trelloService.UpdateStatus += (s, args) =>
                    {
                        if (!cardId2Issue.ContainsKey(args.CardId) ||
                            !mapperStatus.ContainsKey(args.CurrentStatus))
                            return;

                        if (cardId2Issue[args.CardId].Status != args.CurrentStatus)
                            redmineService.Enqueue(new UpdateIssueTask(cardId2Issue[args.CardId].IssueId, mapperStatus[args.CurrentStatus]));
                    };

                    gitlabService.Error += (s, error) => Console.WriteLine(error);

                    gitlabService.Start();
                    redmineService.Start();
                    trelloService.Start();

                    while (true)
                    {
                        var keyInfo = Console.ReadKey();
                        if (keyInfo.Key == ConsoleKey.Q)
                            break;
                    }

                    trelloService.Stop();
                    gitlabService.Stop();
                    redmineService.Stop();
                }
            }
            finally
            {
                JsonConfig.Write(trelloOptions, TRELLO_OPTIONS_FILE).Wait();
                JsonConfig.Write(gitlabOptions, GITLAB_OPTIONS_FILE).Wait();
                JsonConfig.Write(redmineOptions, REDMINE_OPTIONS_FILE).Wait();
            }
        }

        static void UptimeCommandAction(UptimeCommand command, CommentEventArgs args)
        {
            redmineService.Enqueue(new UpdateWorkTimeTask(cardId2Issue[args.CardId].IssueId, command.Hours, command.Comment,
                result =>
                {
                    trelloService.Enqueue(new EmojiCommentTask(args.CardId, args.CommentId,
                        result ? Emojis.WhiteCheckMark : Emojis.FaceWithSymbolsOnMouth));
                }));
        }
    }
}
