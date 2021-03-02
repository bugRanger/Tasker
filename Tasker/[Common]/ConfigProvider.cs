namespace Tasker
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using NLog;

    using Services.Gitlab;
    using Services.Trello;
    using Services.Redmine;

    using Framework.Common;

    using System.Collections.Generic;

    public class ConfigProvider : IConfigProvider
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

        private const string SETTING_SERVICE_FILE = "serviceSettings.json";

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly object _locker = new object();
        private readonly Dictionary<IConfigContainer, string> _configToFile;

        #endregion Fields

        #region Properties

        public SettingServices Settings { get; private set; }

        #endregion Properties

        #region Constructors

        public ConfigProvider()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _configToFile = new Dictionary<IConfigContainer, string>();
        }

        #endregion Constructors

        #region Methods

        public void Load()
        {
            lock (_locker)
            {
                var tasks = _configToFile
                    .Select((s) => s.Key.Load(this))
                    .AsParallel()
                    .ToArray();

                Task.Run(async () => { await Handle("loading configuration", async () => Settings = await JsonConfig.Read<SettingServices>(SETTING_SERVICE_FILE)); }).Wait();
                Task.Factory
                    .ContinueWhenAll(tasks, s => HandleContinue("Load", s))
                    .Wait();
            }
        }

        public void Save()
        {
            lock (_locker)
            {
                var tasks = _configToFile
                    .Select(s => s.Key.Save(this))
                    .AsParallel()
                    .ToArray();
                
                Task.Run(async () => { await Handle("saving configuration", async () => await JsonConfig.Write(Settings, SETTING_SERVICE_FILE)); }).Wait();
                Task.Factory
                    .ContinueWhenAll(tasks, s => HandleContinue("Save", s))
                    .Wait();
            }
        }

        public void Register(IConfigContainer config, string fileName)
        {
            _configToFile[config] = fileName;
        }

        public async Task<T> Read<T>(IConfigContainer config, CancellationToken token = default)
            where T : class, new()
        {
            if (!_configToFile.TryGetValue(config, out string configFile))
            {
                return await Task.FromResult(default(T));
            }

            return await JsonConfig.Read<T>(configFile, token);
        }

        public async Task Write<T>(IConfigContainer config, T value, CancellationToken token = default)
            where T : class, new()
        {
            if (!_configToFile.TryGetValue(config, out string configFile))
            {
                return;
            }

            await JsonConfig.Write(value, configFile, token);
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

        private void HandleContinue(string action, Task<IConfigContainer>[] tasks)
        {
            foreach (var task in tasks)
            {
                if (task.IsFaulted && _configToFile.TryGetValue(task.Result, out string file))
                {
                    _logger.Error(() => $"{action} config `{file}` exception: " + task.Exception);
                }
            }
        }

        #endregion Methods
    }
}
