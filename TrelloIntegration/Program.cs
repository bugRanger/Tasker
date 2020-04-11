namespace TrelloIntegration
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using TrelloIntegration.Services;
    using TrelloIntegration.Services.Redmine.Tasks;
    using TrelloIntegration.Services.Trello.Tasks;

    partial class Program
    {

        static void Main(string[] args)
        {
            var mapperStatus = new Dictionary<string, int>();
            var cardId2Issue = new Dictionary<string, IssueEntity>();

            // TODO Replace args to config file.
            RedmineOptions redmineOptions = null;
            redmineOptions = new RedmineOptions
            {
                Host = "https://orpo-redmine.elcom.local",
                ApiKey = "eec8386ba3d9331095224f88f8001d5af31c07bc",
                Interval = 100,
                UserId = 163,
                EstimatedHoursABS = 8,
            };
            //Parser.Default
            //    .ParseArguments<RedmineOptions>(args)
            //    .WithParsed(opts => redmineOptions = opts);

            TrelloOptions trelloOptions = null;
            trelloOptions = new TrelloOptions()
            {
                AppKey = "dc215d5027ab3a15a00f77c98003867b",
                Token = "2279e282db22528326b881a4d77273f1241d0486ff60da3f303509b736a82937",
                Interval = 100,
            };
            //Parser.Default
            //    .ParseArguments<TrelloOptions>(args)
            //    .WithParsed(opts => trelloOptions = opts);

            using (var trello = new TrelloService(trelloOptions))
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
                                if (mapperStatus[args.StatusOld] == 22)
                                    redmine.Enqueue(new UpdateWorkTimeTask(cardId2Issue[args.CardId].IssueId));
                            }));
                };

                redmine.Start();
                trello.Start();

                while (true)
                {
                    var keyInfo = Console.ReadKey();

                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.Q:
                            break;
                    }
                }
            }
        }
    }
}
