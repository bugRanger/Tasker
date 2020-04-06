namespace Redmine2Trello
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Redmine2Trello.Services;
    using Redmine2Trello.Services.Redmine.Tasks;
    using Redmine2Trello.Services.Trello.Tasks;

    partial class Program
    {

        static void Main(string[] args)
        {
            var cardId2Issue = new Dictionary<string, IssueCard>();
            var issueId2Card = new Dictionary<int, IssueCard>();

            var mapperStatus = new Dictionary<string, int>();

            // TODO Replace args to config file.
            RedmineOptions redmineOptions = null;
            redmineOptions = new RedmineOptions
            {
                Host = "https://orpo-redmine.elcom.local",
                ApiKey = "eec8386ba3d9331095224f88f8001d5af31c07bc",
                Interval = 100,
                AssignedId = 163,
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
                BoardIds = new List<string>() { "5e817523f06574698d25cbdc" }
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
                        if (!issueId2Card.ContainsKey(issue.Id) || mapperStatus[issueId2Card[issue.Id].Status] != issue.Status.Id)
                            trello.Enqueue(new ImportIssueTask(new IssueCard() 
                            {
                                IssueId = issue.Id,
                                Project = issue.Project.Name,
                                Subject = issue.Subject,
                                Status = issue.Status.Name,
                            }));
                    }
                };

                trello.NewBoard += (s, board) =>
                {
                    // TODO Add filter for status.
                    trello.Enqueue(new UpdateListTask(board.Id, mapperStatus.Keys.ToArray()));
                };
                trello.ImportCard += (s, args) =>
                {
                    issueId2Card[args.IssueId] = args;
                    cardId2Issue[args.CardId] = args;
                };
                trello.UpdateCard += (s, card) =>
                {
                    if (!cardId2Issue.ContainsKey(card.Id))
                        return;

                    if (cardId2Issue[card.Id].Status != card.List.Name)
                        redmine.Enqueue(new UpdateIssueTask(cardId2Issue[card.Id].IssueId, mapperStatus[card.List.Name]));
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
