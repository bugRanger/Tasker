﻿namespace Tasker
{
    using System;

    using NLog;

    using Framework.Timeline;

    using Services.Trello;
    using Services.Gitlab;
    using Services.Redmine;

    partial class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration ??= new NLog.Config.LoggingConfiguration();

            ConfigProvider config = null;
            TaskController service = null;
            TaskContainer container = null;

            bool stopped = false;
            try
            {
                container = new TaskContainer();

                config = new ConfigProvider();
                config.Register(container, "taskCached.json");
                config.Load();

                service = new TaskController(container);
                service.Register(new TrelloService(config.Settings.TrelloOptions, TimelineEnvironment.Instance));
                service.Register(new GitLabService(config.Settings.GitLabOptions, TimelineEnvironment.Instance));
                service.Register(new RedmineService(config.Settings.RedmineOptions, TimelineEnvironment.Instance));

                service.Start();

                while (!stopped)
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
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                service.Stop();
                config.Save();
            }
        }
    }
}
