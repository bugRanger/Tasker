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

            ConfigProvider config = null;
            TaskerService service = null;

            try
            {
                config = new ConfigProvider();
                config.Load();

                service = new TaskerService(config);
                service.SetStrategy(new TaskerStrategy(config));

                service.Register(new TrelloService(config.TrelloOptions, TimelineEnviroment.Instance));
                service.Register(new GitLabService(config.GitLabOptions, TimelineEnviroment.Instance));
                service.Register(new RedmineService(config.RedmineOptions, TimelineEnviroment.Instance));

                service.Start();

                while (true)
                {
                    Console.WriteLine("Press key Q for stopped");

                    var keyInfo = Console.ReadKey();
                    if (keyInfo.Key == ConsoleKey.Q)
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                service?.Stop();
                config?.Save();
            }
        }
    }
}
