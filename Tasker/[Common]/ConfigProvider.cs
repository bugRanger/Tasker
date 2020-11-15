namespace Tasker
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using NLog;

    using Services.Gitlab;
    using Services.Trello;
    using Services.Redmine;

    using Framework.Common;

    using Tasker.Common.Task;

    using System.Collections.Generic;
    using System.Collections.Concurrent;

    public class ConfigProvider
    {
        #region Classes

        public class SettingServices
        {
            #region Fields

            private TrelloOptions _trelloOptions;
            private GitLabOptions _gitLabOptions;
            private RedmineOptions _redmineOptions;

            #endregion Fields

            #region Properties

            public TrelloOptions TrelloOptions { get => _trelloOptions; set => _trelloOptions = value ?? _trelloOptions; }

            public GitLabOptions GitLabOptions { get => _gitLabOptions; set => _gitLabOptions = value ?? _gitLabOptions; }

            public RedmineOptions RedmineOptions { get => _redmineOptions; set => _redmineOptions = value ?? _redmineOptions; }

            #endregion Properties

            public SettingServices() 
            {
                _trelloOptions = new TrelloOptions();
                _gitLabOptions = new GitLabOptions();
                _redmineOptions = new RedmineOptions();
            }
        }

        #endregion Classes

        #region Constants

        private const string TASK_CACHED_FILE = "taskCached.json";
        private const string SETTING_SERVICE_FILE = "serviceSettings.json";

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly object _locker = new object();

        #endregion Fields

        #region Properties

        public SettingServices Settings { get; private set; }

        public IDictionary<int, TaskCommon> Tasks { get; private set; }

        #endregion Properties

        #region Constructors

        public ConfigProvider()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        #endregion Constructors

        #region Methods

        public void Load()
        {
            lock (_locker)
            {
                Task.Factory
                    .ContinueWhenAll(
                        new[]
                        {
                            Task.Run(async () => { await Handle("loading task cached", async () => Tasks = new ConcurrentDictionary<int, TaskCommon>(await JsonConfig.Read<List<KeyValuePair<int, TaskCommon>>>(TASK_CACHED_FILE))); }),
                            Task.Run(async () => { await Handle("loading configuration", async () => Settings = await JsonConfig.Read<SettingServices>(SETTING_SERVICE_FILE)); }),
                        }, 
                        s => { })
                    .Wait();
            }
        }

        public void Save()
        {
            lock (_locker)
            {
                Task.Factory
                    .ContinueWhenAll(
                        new[]
                        {
                            Task.Run(async () => { await Handle("saving task cached", async () => await JsonConfig.Write(Tasks.ToArray(), TASK_CACHED_FILE)); }),
                            Task.Run(async () => { await Handle("saving configuration", async () => await JsonConfig.Write(Settings, SETTING_SERVICE_FILE)); }),
                        }, 
                        s => { })
                    .Wait();
            }
        }

        private async Task Handle(string title, Func<Task> action)
        {
            try
            {
                await action();

                _logger.Info($"success {title} {title}");
            }
            catch (Exception ex)
            {
                _logger.Error($"failure {title}: " + ex.Message);
            }
        }

        #endregion Methods
    }
}
