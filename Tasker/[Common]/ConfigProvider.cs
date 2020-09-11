namespace Tasker
{
    using System;
    using System.Threading.Tasks;

    using Framework.Common;

    using Services.GitLab;
    using Services.Trello;
    using Services.Redmine;

    // TODO: Уйти от конфиг файлов и статик класса.
    public class ConfigProvider : IServiceMapper
    {
        #region Constants

        private const string GITLAB_OPTIONS_FILE = "gitlabOptions.json";
        private const string TRELLO_OPTIONS_FILE = "trelloOptions.json";
        private const string REDMINE_OPTIONS_FILE = "redmineOptions.json";

        private const string CARD_MAPPER_FILE = "cardsMapper.json";
        private const string LIST_MAPPER_FILE = "listsMapper.json";
        private const string LABEL_MAPPER_FILE = "labelMapper.json";
        private const string FIELD_MAPPER_FILE = "fieldMapper.json";
        private const string BRANCH_MAPPER_FILE = "branchesMapper.json";

        #endregion Constants

        #region Fields

        private static readonly object _locker = new object();

        private static ConfigProvider _instance;

        #endregion Fields

        #region Properties

        public static ConfigProvider Instance 
        { 
            get 
            {
                lock (_locker) 
                {
                    return _instance ??= new ConfigProvider();
                }
            } 
        }

        public ITrelloOptions TrelloOptions { get; private set; }

        public IGitLabOptions GitLabOptions { get; private set; }

        public IRedmineOptions RedmineOptions { get; private set; }

        public Mapper<string, int> Card2IssueMapper { get; private set; }

        public Mapper<string, int> List2StatusMapper { get; private set; }

        public Mapper<string, int> Label2ProjectMapper { get; private set; }

        public Mapper<string, int> Branch2IssueMapper { get; private set; }

        public Mapper<string, CustomField> Field2FieldMapper { get; private set; }

        #endregion Properties

        #region Methods

        public void Load()
        {
            lock (_locker)
            {
                Task.Factory
                    .ContinueWhenAll(new[]
                    {
                        Task.Run(async () => { TrelloOptions = await JsonConfig.Read<TrelloOptions>(TRELLO_OPTIONS_FILE); }),
                        Task.Run(async () => { GitLabOptions = await JsonConfig.Read<GitLabOptions>(GITLAB_OPTIONS_FILE); }),
                        Task.Run(async () => { RedmineOptions = await JsonConfig.Read<RedmineOptions>(REDMINE_OPTIONS_FILE); }),

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
                        Task.Run(async () => await JsonConfig.Write(TrelloOptions, TRELLO_OPTIONS_FILE)),
                        Task.Run(async () => await JsonConfig.Write(GitLabOptions, GITLAB_OPTIONS_FILE)),
                        Task.Run(async () => await JsonConfig.Write(RedmineOptions, REDMINE_OPTIONS_FILE)),

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
