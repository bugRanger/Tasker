namespace Redmine2Trello
{
    using System;
    using CommandLine;

    using Redmine2Trello.Services;

    class Program
    {
        static void Main(string[] args)
        {
            // TODO Replace args to config file.
            RedmineOptions redmineOptions = null;
            redmineOptions = new RedmineOptions
            {
                Host = "https://orpo-redmine.elcom.local",
                ApiKey = "eec8386ba3d9331095224f88f8001d5af31c07bc",
                AssignedId = 163,
                StatusIds = new[] { 1 },
            };
            //Parser.Default
            //    .ParseArguments<RedmineOptions>(args)
            //    .WithParsed(opts => redmineOptions = opts);

            TrelloOptions trelloOptions = null;
            trelloOptions = new TrelloOptions()
            {
                AppKey = "dc215d5027ab3a15a00f77c98003867b",
                Token = "2279e282db22528326b881a4d77273f1241d0486ff60da3f303509b736a82937"
            };
            //Parser.Default
            //    .ParseArguments<TrelloOptions>(args)
            //    .WithParsed(opts => trelloOptions = opts);

            using (var trello = new TrelloService(trelloOptions))
            using (var redmine = new RedmineService(redmineOptions))
            {
                trello.Start();

                //redmine.UpdateIssues += (s, e) => { };
                //redmine.Start();

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
