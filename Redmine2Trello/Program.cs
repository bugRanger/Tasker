namespace Redmine2Trello
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Redmine2Trello.Services;
    using Redmine2Trello.Services.Redmine.Tasks;
    using Redmine2Trello.Services.Trello.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            var card2Issues = new Dictionary<string, int>();
            var issues2Card = new Dictionary<int, string>();

            var mapperStatus = new Dictionary<string, int>();

            // TODO Replace args to config file.
            RedmineOptions redmineOptions = null;
            redmineOptions = new RedmineOptions
            {
                Host = "https://orpo-redmine.elcom.local",
                ApiKey = "eec8386ba3d9331095224f88f8001d5af31c07bc",
                Interval = 300,
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
                Interval = 300,
                BoardIds = new[] { "5e817523f06574698d25cbdc" }
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
                        issues2Card[issue.Id] = null;
                        trello.Enqueue(new ImportIssueTask(issue.Project.Name, issue.Subject, issue.Status.Name));
                    }
                };
                trello.NewBoard += (s, board) => 
                {
                    trello.Enqueue(new UpdateListTask(board.Id, mapperStatus.Keys.ToArray()));
                };
                trello.UpdateCards += (s, cards) => 
                {
                    try
                    {
                        foreach (var card in cards)
                        {
                            redmine.Enqueue(new UpdateIssueTask(card2Issues[card.Id], mapperStatus[card.List.Name]));
                        }
                    }
                    catch 
                    {
                        // Ignore.
                    }
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
