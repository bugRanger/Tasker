namespace Tasker
{
    using System;
    using System.Threading.Tasks;

    using NLog;

    using Framework.Common;

    using Services.GitLab;
    using Services.Trello;
    using Services.Redmine;

    public class ConfigProvider : IServiceMapper
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

        private const string SETTING_SERVICEs_FILE = "serviceSettings.json";

        // TODO: Move to data base.
        private const string CARD_MAPPER_FILE = "cardsMapper.json";
        private const string LIST_MAPPER_FILE = "listsMapper.json";
        private const string LABEL_MAPPER_FILE = "labelMapper.json";
        private const string FIELD_MAPPER_FILE = "fieldMapper.json";
        private const string BRANCH_MAPPER_FILE = "branchesMapper.json";

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly object _locker = new object();

        #endregion Fields

        #region Properties

        public SettingServices Settings { get; private set; }

        public Mapper<string, int> Card2IssueMapper { get; private set; }

        public Mapper<string, int> List2StatusMapper { get; private set; }

        public Mapper<string, int> Label2ProjectMapper { get; private set; }

        public Mapper<string, int> Branch2IssueMapper { get; private set; }

        public Mapper<string, CustomField> Field2FieldMapper { get; private set; }

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
                    .ContinueWhenAll(new[]
                    {
                        Task.Run(async () => 
                        {
                            try
                            {
                                Settings = await JsonConfig.Read<SettingServices>(SETTING_SERVICEs_FILE);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error("failure loading configuration: " + ex.Message);
                            }
                        }),

                        Task.Run(async () => { Card2IssueMapper = await JsonConfig.Read<Mapper<string, int>>(CARD_MAPPER_FILE); }),
                        Task.Run(async () => { List2StatusMapper = await JsonConfig.Read<Mapper<string, int>>(LIST_MAPPER_FILE); }),
                        Task.Run(async () => { Label2ProjectMapper = await JsonConfig.Read<Mapper<string, int>>(LABEL_MAPPER_FILE); }),
                        Task.Run(async () => { Branch2IssueMapper = await JsonConfig.Read<Mapper<string, int>>(BRANCH_MAPPER_FILE); }),
                        Task.Run(async () => { Field2FieldMapper = await JsonConfig.Read<Mapper<string, CustomField>>(FIELD_MAPPER_FILE); }),
                    }, s => { })
                    .Wait();
            }
        }

        public void Save()
        {
            lock (_locker)
            {
                Task.Factory
                    .ContinueWhenAll(new[]
                    {
                        Task.Run(async () => await JsonConfig.Write(Settings, SETTING_SERVICEs_FILE)),

                        Task.Run(async () => await JsonConfig.Write(Card2IssueMapper, CARD_MAPPER_FILE)),
                        Task.Run(async () => await JsonConfig.Write(List2StatusMapper, LIST_MAPPER_FILE)),
                        Task.Run(async () => await JsonConfig.Write(Label2ProjectMapper, LABEL_MAPPER_FILE)),
                        Task.Run(async () => await JsonConfig.Write(Branch2IssueMapper, BRANCH_MAPPER_FILE)),
                        Task.Run(async () => await JsonConfig.Write(Field2FieldMapper, FIELD_MAPPER_FILE)),
                    }, s => { })
                    .Wait();
            }
        }

        #endregion Methods
    }
}
