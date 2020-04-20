namespace TrelloIntegration
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

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

        private static TrelloOptions _trelloOptions;
        private static TrelloService _trelloService;
        private static CommandController _trelloCommand;

        private static GitLabOptions _gitlabOptions;
        private static GitLabService _gitlabService;

        private static RedmineOptions _redmineOptions;
        private static RedmineService _redmineService;

        /// <summary>
        /// Trello list convert to status issue redmine.
        /// </summary>
        private static Dictionary<string, int> _statusesMapper;
        /// <summary>
        /// Trello card id convert to issue Redmine.
        /// </summary>
        private static Dictionary<string, IssueEntity> _issueMapper;

        static void Main(string[] args)
        {
            _issueMapper = new Dictionary<string, IssueEntity>();
            _statusesMapper = new Dictionary<string, int>();

            _trelloOptions = JsonConfig.Read<TrelloOptions>(TRELLO_OPTIONS_FILE).Result;
            _gitlabOptions = JsonConfig.Read<GitLabOptions>(GITLAB_OPTIONS_FILE).Result;
            _redmineOptions = JsonConfig.Read<RedmineOptions>(REDMINE_OPTIONS_FILE).Result;

            try
            {
                _trelloCommand = new CommandController(() => $"^{_trelloService.Mention} ([A-Za-z]+):");
                _trelloCommand.Register<MergeCommand, CommentEventArgs>("merge", MergeCommandAction);
                _trelloCommand.Register<UptimeCommand, CommentEventArgs>("update", UptimeCommandAction);

                using (_trelloService = new TrelloService(_trelloOptions))
                using (_gitlabService = new GitLabService(_gitlabOptions))
                using (_redmineService = new RedmineService(_redmineOptions))
                {
                    _redmineService.Error += (s, error) => Console.WriteLine(error);
                    _redmineService.UpdateStatuses += OnRedmineService_UpdateStatuses;
                    _redmineService.UpdateIssues += OnRedmineService_UpdateIssues;

                    _trelloService.Error += (s, error) => Console.WriteLine(error);
                    _trelloService.UpdateComments += OnTrelloService_UpdateComments;
                    _trelloService.UpdateStatus += OnTrelloService_UpdateStatus;

                    _gitlabService.Error += (s, error) => Console.WriteLine(error);
                    _gitlabService.UpdateRequests += OnGitlabService_UpdateRequests;

                    //_gitlabService.Start();
                    _redmineService.Start();
                    _trelloService.Start();

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
            if (!_issueMapper.TryGetValue(args.CardId, out var issue))
                return;

            _redmineService.Enqueue(
                new UpdateWorkTimeTask(
                    issue.IssueId,
                    command.Hours,
                    command.Comment,
                    result => OnCallbackCommandTask(args.CardId, args.CommentId, result)));
        }

        static void OnCallbackCommandTask(string cardId, string commendId, bool result)
        {
            _trelloService.Enqueue(
                new EmojiCommentTask(cardId, commendId,
                    result
                        ? Emojis.WhiteCheckMark
                        : Emojis.FaceWithSymbolsOnMouth));
        }

        #region Trello

        static void OnTrelloService_UpdateStatus(object sender, StatusEventArgs args)
        {
            if (!_issueMapper.ContainsKey(args.CardId) ||
                !_statusesMapper.ContainsKey(args.CurrentStatus))
                return;

            if (_issueMapper[args.CardId].Status != args.CurrentStatus)
                _redmineService.Enqueue(new UpdateIssueTask(_issueMapper[args.CardId].IssueId, _statusesMapper[args.CurrentStatus]));
        }

        static void OnTrelloService_UpdateComments(object sender, CommentEventArgs args)
        {
            if (!_issueMapper.ContainsKey(args.CardId))
                return;

            _trelloCommand.TryAction(args.Text, args);
        }

        #endregion Trello

        #region Redmine

        static void OnRedmineService_UpdateIssues(object sender, Issue[] issues)
        {
            // TODO Добавить проверку на новую доску с пересозданием.
            _trelloService.Enqueue(
                new UpdateBoardTask(
                    _trelloOptions.BoardName, 
                    _trelloOptions.BoardId,
                    callback: boardId =>
                    {
                        // TODO Repeat!
                        if (string.IsNullOrWhiteSpace(boardId))
                            return;

                        _trelloOptions.BoardId = boardId;
                        try
                        {
                            // Refresh lists.
                            _trelloService.Enqueue(
                                new UpdateListTask(
                                    _trelloOptions.BoardId,
                                    _statusesMapper.Keys.ToArray()));                            
                        }
                        finally
                        {
                            JsonConfig.Write(_trelloOptions, TRELLO_OPTIONS_FILE).Wait();
                        }
                    }));

            // Update cards.
            foreach (var issue in issues)
            {
                // TODO Add more detail for card issue.
                _trelloService.Enqueue(
                    new UpdateCardTask(
                        _trelloOptions.BoardId,
                        $"[{issue.Id}] {issue.Subject}",
                        issue.Description,
                        issue.Status.Name,
                        _statusesMapper.Keys.ToArray(),
                        cardId =>
                        {
                            if (string.IsNullOrWhiteSpace(cardId))
                                return;

                            _issueMapper[cardId] =
                                new IssueEntity()
                                {
                                    CardId = cardId,
                                    IssueId = issue.Id,
                                    Project = issue.Project.Name,
                                    Subject = $"[{issue.Id}] {issue.Subject}",
                                    Discription = issue.Description,
                                    Status = issue.Status.Name,
                                    UpdateDT = issue.UpdatedOn ?? issue.CreatedOn,
                                };
                        }));
            }
        }

        static void OnRedmineService_UpdateStatuses(object sender, IssueStatus[] statuses)
        {
            if (_redmineOptions.Statuses == null)
                return;

            var dict = statuses.ToDictionary(k => k.Id, v => v);

            foreach (int statusId in _redmineOptions.Statuses)
            {
                if (dict.TryGetValue(statusId, out var issueStatus))
                    _statusesMapper[issueStatus.Name] = issueStatus.Id;
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
