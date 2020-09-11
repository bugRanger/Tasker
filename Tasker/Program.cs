namespace Tasker
{
    using System;

    using NLog;

    using Framework.Common;

    using Services.Trello;
    using Services.GitLab;
    using Services.Redmine;

    partial class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration ??= new NLog.Config.LoggingConfiguration();

            try
            {
                ConfigProvider.Instance.Load();

                var strategy = new TaskerStrategy(ConfigProvider.Instance);

                using var trelloService = new TrelloService(strategy, ConfigProvider.Instance.TrelloOptions, TimelineEnviroment.Instance);
                using var gitlabService = new GitLabService(strategy, ConfigProvider.Instance.GitLabOptions, TimelineEnviroment.Instance);
                using var redmineService = new RedmineService(strategy, ConfigProvider.Instance.RedmineOptions, TimelineEnviroment.Instance);

                Console.WriteLine("Starting...");

                strategy.Start();

                Console.WriteLine("Starting success!");

                while (true)
                {
                    Console.WriteLine("Press key Q for stopped");

                    var keyInfo = Console.ReadKey();
                    if (keyInfo.Key == ConsoleKey.Q)
                        break;
                }

                Console.WriteLine("Stopped...");

                strategy.Stop();

                Console.WriteLine("Stopped success!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                ConfigProvider.Instance.Save();
            }
        }
    }
}
