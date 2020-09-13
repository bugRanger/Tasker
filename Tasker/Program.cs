namespace Tasker
{
    using System;

    using NLog;

    using Framework.Timeline;

    using Services.Trello;
    using Services.GitLab;
    using Services.Redmine;

    partial class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration ??= new NLog.Config.LoggingConfiguration();

            ConfigProvider config = null;
            ITaskerService service = null;
            ITaskerStrategy strategy = null;

            bool stopped = false;
            try
            {
                config = new ConfigProvider();
                config.Load();

                strategy = new TaskerStrategy(config);

                service = new TaskerService(config);
                service.Register(new TrelloService(config.Settings.TrelloOptions, TimelineEnvironment.Instance));
                service.Register(new GitLabService(config.Settings.GitLabOptions, TimelineEnvironment.Instance));
                service.Register(new RedmineService(config.Settings.RedmineOptions, TimelineEnvironment.Instance));

                while (!stopped)
                {
                    var restart = false;

                    service.Start(strategy);

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
                                    //strategy new ConfigurationStrategy();
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
