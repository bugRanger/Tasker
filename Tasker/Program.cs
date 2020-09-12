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
            bool stopped = false;
            try
            {
                config = new ConfigProvider();
                config.Load();

                service = new TaskerService(config);
                service.SetStrategy(new TaskerStrategy(config));

                service.Register(new TrelloService(config.TrelloOptions, TimelineEnviroment.Instance));
                service.Register(new GitLabService(config.GitLabOptions, TimelineEnviroment.Instance));
                service.Register(new RedmineService(config.RedmineOptions, TimelineEnviroment.Instance));

                while (!stopped)
                {
                    var restart = false;

                    service.Start();

                    try
                    {
                        while (!restart && !stopped)
                        {
                            Console.WriteLine("Press key Q for stopped");
                            Console.WriteLine("Press key S for setting");

                            var keyInfo = Console.ReadKey();

                            switch (keyInfo.Key)
                            {
                                case ConsoleKey.Q:
                                    stopped = true;
                                    break;

                                case ConsoleKey.S:
                                    // TODO: Impl.
                                    //service.SetStrategy(new ConfigurationStrategy());
                                    //restart = true;
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    finally 
                    {
                        service.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                config?.Save();
            }
        }
    }
}
