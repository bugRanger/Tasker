using System.Threading.Tasks;

namespace Tasker
{
    using System;
    using System.Threading.Tasks;

    using NLog;

    using Services.GitLab;
    using Services.Trello;
    using Services.Redmine;

    using Framework.Common;

    using System.Collections.Generic;

    public class ConfigProvider
    {
        #region Classes

        public class SettingServices : ISettingServices
        {
            public TrelloOptions TrelloOptions { get; set; }

            public GitLabOptions GitLabOptions { get; set; }

            public RedmineOptions RedmineOptions { get; set; }

            ITrelloOptions ISettingServices.TrelloOptions => TrelloOptions;

            IGitLabOptions ISettingServices.GitLabOptions => GitLabOptions;

            IRedmineOptions ISettingServices.RedmineOptions => RedmineOptions;
        }

        #endregion Classes

        #region Constants

        private const string CACHED_FILE = "cached.json";
        private const string SETTING_SERVICE_FILE = "serviceSettings.json";

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly object _locker = new object();

        #endregion Fields

        #region Properties

        public SettingServices Settings { get; private set; }

        public List<KeyValuePair<int, string>> Cached { get; private set; }

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
                            Task.Run(async () => { await Handle("loading cached", async () => Cached = await JsonConfig.Read<List<KeyValuePair<int, string>>>(CACHED_FILE)); }),
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
                            Task.Run(async () => { await Handle("saving cached", async () => await JsonConfig.Write(Cached, CACHED_FILE)); }),
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
