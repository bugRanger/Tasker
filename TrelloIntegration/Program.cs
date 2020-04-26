namespace TrelloIntegration
{
    using System;
    using System.Linq;

    using Manatee.Trello;
    using RedmineApi.Core.Types;
    using GitLabApiClient.Models.MergeRequests.Responses;

    using TrelloIntegration.Common;
    using TrelloIntegration.Common.Command;

    using TrelloIntegration.Services;

    using TrelloIntegration.Services.Trello;
    using TrelloIntegration.Services.Trello.Tasks;
    using TrelloIntegration.Services.Trello.Commands;

    using TrelloIntegration.Services.GitLab;
    using TrelloIntegration.Services.GitLab.Tasks;

    using TrelloIntegration.Services.Redmine;
    using TrelloIntegration.Services.Redmine.Tasks;

    partial class Program
    {
        const string GITLAB_OPTIONS_FILE = "gitlabOptions.json";
        const string TRELLO_OPTIONS_FILE = "trelloOptions.json";
        const string REDMINE_OPTIONS_FILE = "redmineOptions.json";

        const string CARDS_MAPPER_FILE = "cardsMapper.json";
        const string LISTS_MAPPER_FILE = "listsMapper.json";
        const string LABEL_MAPPER_FILE = "labelMapper.json";

        private static TrelloOptions _trelloOptions;
        private static TrelloService _trelloService;
        private static CommandController _trelloCommand;

        private static GitLabOptions _gitlabOptions;
        private static GitLabService _gitlabService;

        private static RedmineOptions _redmineOptions;
        private static RedmineService _redmineService;

        /// <summary>
        /// Trello card id convert to issue Redmine.
        /// </summary>
        private static Mapper<string, int> _card2IssueMapper;

        /// <summary>
        /// Trello list convert to status issue redmine.
        /// </summary>
        private static Mapper<string, int> _list2StatusMapper;

        /// <summary>
        /// Trello label convert to redmine projects.
        /// </summary>
        private static Mapper<string, int> _label2ProjectMapper;

        static void Main(string[] args)
        {
            _card2IssueMapper = JsonConfig.Read<Mapper<string, int>>(CARDS_MAPPER_FILE).Result;
            _list2StatusMapper = JsonConfig.Read<Mapper<string, int>>(LISTS_MAPPER_FILE).Result;
            _label2ProjectMapper = JsonConfig.Read<Mapper<string, int>>(LABEL_MAPPER_FILE).Result;

            _trelloOptions = JsonConfig.Read<TrelloOptions>(TRELLO_OPTIONS_FILE).Result;
            _gitlabOptions = JsonConfig.Read<GitLabOptions>(GITLAB_OPTIONS_FILE).Result;
            _redmineOptions = JsonConfig.Read<RedmineOptions>(REDMINE_OPTIONS_FILE).Result;

            try
            {
                _trelloCommand = new CommandController(() => $"^{_trelloService.Mention} ([A-Za-z]+):");
                _trelloCommand.Register<MergeCommand, CommentEventArgs>("merge", MergeCommandAction);
                _trelloCommand.Register<UptimeCommand, CommentEventArgs>("uptime", UptimeCommandAction);

                using (_trelloService = new TrelloService(_trelloOptions))
                using (_gitlabService = new GitLabService(_gitlabOptions))
                using (_redmineService = new RedmineService(_redmineOptions))
                {
                    _redmineService.Error += (s, error) => Console.WriteLine(error);
                    _redmineService.UpdateStatuses += OnRedmineService_UpdateStatuses;
                    _redmineService.UpdateIssues += OnRedmineService_UpdateIssues;
                    _redmineService.UpdateProjects += OnRedmine_UpdateProjects;

                    _trelloService.Error += (s, error) => Console.WriteLine(error);
                    _trelloService.UpdateComments += OnTrelloService_UpdateComments;
                    _trelloService.UpdateStatus += OnTrelloService_UpdateStatus;

                    _gitlabService.Error += (s, error) => Console.WriteLine(error);
                    _gitlabService.UpdateRequests += OnGitlabService_UpdateRequests;

                    _trelloService.Start();
                    _trelloService.Enqueue(new UpdateBoardTask(
                        id: _trelloOptions.BoardId,
                        name: _trelloOptions.BoardName, 
                        clear: string.IsNullOrWhiteSpace(_trelloOptions.BoardId) || _card2IssueMapper.Count == 0,
                        callback: boardId =>
                        {
                            _trelloOptions.BoardId = boardId;

                            _redmineService.Start();
                            //_gitlabService.Start();
                        }));

                    while (true)
                    {
                        var keyInfo = Console.ReadKey();
                        if (keyInfo.Key == ConsoleKey.Q)
                            break;
                    }

                    _trelloService.Stop();
                    _gitlabService.Stop();
                    _redmineService.Stop();
                }
            }
            finally
            {
                JsonConfig.Write(_card2IssueMapper, CARDS_MAPPER_FILE).Wait();
                JsonConfig.Write(_list2StatusMapper, LISTS_MAPPER_FILE).Wait();
                JsonConfig.Write(_label2ProjectMapper, LABEL_MAPPER_FILE).Wait();

                JsonConfig.Write(_trelloOptions, TRELLO_OPTIONS_FILE).Wait();
                JsonConfig.Write(_gitlabOptions, GITLAB_OPTIONS_FILE).Wait();
                JsonConfig.Write(_redmineOptions, REDMINE_OPTIONS_FILE).Wait();
            }
        }

        static void MergeCommandAction(MergeCommand command, CommentEventArgs args)
        {
            // TODO Get project id for GitLab MR.
            _gitlabService.Enqueue(
                new UpdateMergeRequestTask(
                    0,
                    command.Source,
                    command.Target,
                    command.Title,
                    result => OnCallbackCommandTask(args.CardId, args.CommentId, result)));
        }

        static void UptimeCommandAction(UptimeCommand command, CommentEventArgs args)
        {
            if (!_card2IssueMapper.TryGetValue(args.CardId, out int issueId))
                return;

            _redmineService.Enqueue(
                new UpdateWorkTimeTask(
                    issueId,
                    command.Hours,
                    command.Comment,
                    result => OnCallbackCommandTask(args.CardId, args.CommentId, result)));
        }

        static void OnCallbackCommandTask(string cardId, string commendId, bool result)
        {
            _trelloService.Enqueue(new EmojiCommentTask(
                cardId: cardId,  
                commentId: commendId,
                emoji: result
                    ? Emojis.WhiteCheckMark
                    : Emojis.FaceWithSymbolsOnMouth));
        }

        #region Trello

        static void OnTrelloService_UpdateStatus(object sender, ListEventArgs args)
        {
            if (!_card2IssueMapper.ContainsKey(args.CardId) ||
                !_list2StatusMapper.ContainsKey(args.CurrListId))
                return;

            _redmineService.Enqueue(new UpdateIssueTask(
                issueId: _card2IssueMapper[args.CardId], 
                statusId: _list2StatusMapper[args.CurrListId],
                callback: result => 
                {
                    if (!result)
                        _trelloService.Enqueue(new UpdateCardTask(boardId: _trelloOptions.BoardId, () => args.CardId, () => args.PrevListId));
                }));
        }

        static void OnTrelloService_UpdateComments(object sender, CommentEventArgs args)
        {
            if (!_card2IssueMapper.ContainsKey(args.CardId))
                return;

            if (_trelloCommand.TryParse(args.Text, out var command))
            {
                bool result = _trelloCommand.TryAction(command, args);

                _trelloService.Enqueue(new EmojiCommentTask(
                    cardId: args.CardId,
                    commentId: args.CommentId,
                    emoji: result
                        ? Emojis.TimerClock
                        : Emojis.Angry));
            }
            else if (command != null)
            {
                // TODO: Command help.
            }
        }

        #endregion Trello

        #region Redmine

        static void OnRedmineService_UpdateIssues(object sender, Issue[] issues)
        {
            // Update cards.
            foreach (var issue in issues)
            {
                _trelloService.Enqueue(new UpdateCardTask(
                    boardId: _trelloOptions.BoardId,
                    subject: $"[{issue.Id}] {issue.Subject}",
                    description: issue.Description,
                    getCardId: () => _card2IssueMapper.TryGetValue(issue.Id, out string cardId) ? cardId : null,
                    getListId: () => _list2StatusMapper.TryGetValue(issue.Status.Id, out string listId) ? listId : null,
                    getLabelId: () => _label2ProjectMapper.TryGetValue(issue.Project.Id, out string labelId) ? labelId : null,
                    callback: cardId =>
                    {
                        if (string.IsNullOrWhiteSpace(cardId))
                            return;

                        _card2IssueMapper.Add(cardId, issue.Id);
                    }));
            }
        }

        static void OnRedmineService_UpdateStatuses(object sender, IssueStatus[] statuses)
        {
            if (_redmineOptions.Statuses == null)
                return;

            var dict = statuses.ToDictionary(k => k.Id, v => v);

            foreach (int statusId in _redmineOptions.Statuses.Reverse())
            {
                if (!dict.TryGetValue(statusId, out var issueStatus))
                    continue;

                string listId = _list2StatusMapper.TryGetValue(statusId, out string list) ? list : null;
                //var position;
                _trelloService.Enqueue(new UpdateListTask(
                    boardId :_trelloOptions.BoardId,
                    listId: listId,
                    name: issueStatus.Name, 
                    callback: listId =>
                    {
                        if (string.IsNullOrWhiteSpace(listId))
                            return;

                        _list2StatusMapper.Add(listId, statusId);
                    }));
            }
        }

        private static void OnRedmine_UpdateProjects(object sender, Project[] projects)
        {
            foreach (var project in projects)
            {
                string labelId = _label2ProjectMapper.TryGetValue(project.Id, out string label) ? label : null;
                _trelloService.Enqueue(new UpdateLabelTask(
                    boardId: _trelloOptions.BoardId,
                    id: labelId,
                    name: project.Name,
                    callback: labelId =>
                    {
                        if (string.IsNullOrWhiteSpace(labelId))
                            return;

                        _label2ProjectMapper.Add(labelId, project.Id); 
                    }));
            }
        }

        #endregion Redmine

        #region Gitlab

        static void OnGitlabService_UpdateRequests(object sender, MergeRequest[] requests)
        {
            foreach (var request in requests)
            {
                // TODO Impl.
            }
        }

        #endregion Gitlab
    }
}
