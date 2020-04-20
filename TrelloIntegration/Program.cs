namespace TrelloIntegration
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

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

    using Manatee.Trello;

    partial class Program
    {
        const string GITLAB_OPTIONS_FILE = "gitlabOptions.json";
        const string TRELLO_OPTIONS_FILE = "trelloOptions.json";
        const string REDMINE_OPTIONS_FILE = "redmineOptions.json";

        private static TrelloService _trelloService;
        private static GitLabService _gitlabService;
        private static RedmineService _redmineService;

        private static CommandController _trelloCommand;

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

            TrelloOptions trelloOptions = JsonConfig.Read<TrelloOptions>(TRELLO_OPTIONS_FILE).Result;
            GitLabOptions gitlabOptions = JsonConfig.Read<GitLabOptions>(GITLAB_OPTIONS_FILE).Result;
            RedmineOptions redmineOptions = JsonConfig.Read<RedmineOptions>(REDMINE_OPTIONS_FILE).Result;

            try
            {
                _trelloCommand = new CommandController(() => $"^{_trelloService.Mention} ([A-Za-z]+):");
                _trelloCommand.Register<MergeCommand, CommentEventArgs>("merge", MergeCommandAction);
                _trelloCommand.Register<UptimeCommand, CommentEventArgs>("update", UptimeCommandAction);

                using (_trelloService = new TrelloService(trelloOptions))
                using (_gitlabService = new GitLabService(gitlabOptions))
                using (_redmineService = new RedmineService(redmineOptions))
                {
                    _redmineService.Error += (s, error) => Console.WriteLine(error);
                    _redmineService.UpdateStatuses += (s, statuses) =>
                    {
                        if (redmineOptions.Statuses == null)
                            return;

                        var dict = statuses.ToDictionary(k => k.Id, v => v);

                        foreach (int statusId in redmineOptions.Statuses)
                        {
                            if (dict.TryGetValue(statusId, out var issueStatus))
                                _statusesMapper[issueStatus.Name] = issueStatus.Id;
                        }
                    };
                    _redmineService.UpdateIssues += (s, issues) =>
                    {
                        // Restore board.
                        if (string.IsNullOrWhiteSpace(trelloOptions.BoardId))
                        {
                            _trelloService.Enqueue(
                                new UpdateBoardTask(
                                    trelloOptions.BoardName,
                                    callback: boardId =>
                                    {
                                        // TODO Repeat!
                                        if (string.IsNullOrWhiteSpace(boardId))
                                            return;

                                        trelloOptions.BoardId = boardId;
                                        try
                                        {
                                            // Refresh lists.
                                            _trelloService.Enqueue(
                                                new UpdateListTask(
                                                    trelloOptions.BoardId, 
                                                    _statusesMapper.Keys.ToArray()));

                                            // Update cards.
                                            foreach (var issue in issues)
                                            {
                                                // TODO Add more detail for card issue.
                                                _trelloService.Enqueue(
                                                    new UpdateCardTask(
                                                        trelloOptions.BoardId,
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
                                        finally
                                        {
                                            JsonConfig.Write(trelloOptions, TRELLO_OPTIONS_FILE).Wait();
                                        }
                                    }));
                        }
                    };

                    _trelloService.Error += (s, error) => Console.WriteLine(error);
                    _trelloService.UpdateComments += (s, args) =>
                    {
                        if (!_issueMapper.ContainsKey(args.CardId))
                            return;

                        _trelloCommand.TryAction(args.Text, args);
                    };
                    _trelloService.UpdateStatus += (s, args) =>
                    {
                        if (!_issueMapper.ContainsKey(args.CardId) ||
                            !_statusesMapper.ContainsKey(args.CurrentStatus))
                            return;

                        if (_issueMapper[args.CardId].Status != args.CurrentStatus)
                            _redmineService.Enqueue(new UpdateIssueTask(_issueMapper[args.CardId].IssueId, _statusesMapper[args.CurrentStatus]));
                    };

                    _gitlabService.Error += (s, error) => Console.WriteLine(error);
                    _gitlabService.UpdateRequests += (s, requests) =>
                    {
                        foreach (var request in requests)
                        {
                        }
                        // TODO Impl.
                    };

                    _gitlabService.Start();
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
                JsonConfig.Write(trelloOptions, TRELLO_OPTIONS_FILE).Wait();
                JsonConfig.Write(gitlabOptions, GITLAB_OPTIONS_FILE).Wait();
                JsonConfig.Write(redmineOptions, REDMINE_OPTIONS_FILE).Wait();
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
    }
}
