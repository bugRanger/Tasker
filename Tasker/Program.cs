namespace TrelloIntegration
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using NLog;

    using Common.Tasks;
    using Common.Command;
    using Framework.Common;

    using Manatee.Trello;
    using RedmineApi.Core.Types;

    using Services.Trello;
    using Services.Trello.Tasks;
    using Services.Trello.Commands;

    using Services.GitLab;
    using Services.GitLab.Tasks;

    using Services.Redmine;
    using Services.Redmine.Tasks;


    using TrelloCustomField = Services.Trello.CustomField;

    partial class Program
    {
        const string GITLAB_OPTIONS_FILE = "gitlabOptions.json";
        const string TRELLO_OPTIONS_FILE = "trelloOptions.json";
        const string REDMINE_OPTIONS_FILE = "redmineOptions.json";

        // TODO: Добавить поддержку работы с базой данных, вместо файликов.
        const string CARD_MAPPER_FILE = "cardsMapper.json";
        const string LIST_MAPPER_FILE = "listsMapper.json";
        const string LABEL_MAPPER_FILE = "labelMapper.json";
        const string FIELD_MAPPER_FILE = "fieldMapper.json";

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

        private static Mapper<string, TrelloCustomField> _field2FieldMapper;

        static void Main(string[] args)
        {
            LogManager.Configuration ??= new NLog.Config.LoggingConfiguration();

            _card2IssueMapper = JsonConfig.Read<Mapper<string, int>>(CARD_MAPPER_FILE).Result;
            _list2StatusMapper = JsonConfig.Read<Mapper<string, int>>(LIST_MAPPER_FILE).Result;
            _label2ProjectMapper = JsonConfig.Read<Mapper<string, int>>(LABEL_MAPPER_FILE).Result;
            _field2FieldMapper = JsonConfig.Read<Mapper<string, TrelloCustomField>>(FIELD_MAPPER_FILE).Result;

            _trelloOptions = JsonConfig.Read<TrelloOptions>(TRELLO_OPTIONS_FILE).Result;
            _gitlabOptions = JsonConfig.Read<GitLabOptions>(GITLAB_OPTIONS_FILE).Result;
            _redmineOptions = JsonConfig.Read<RedmineOptions>(REDMINE_OPTIONS_FILE).Result;

            try
            {
                _trelloCommand = new CommandController(() => $"^{_trelloService.Mention} ([A-Za-z]+):");
                _trelloCommand.Register<MergeCommand, CommentEventArgs>("merge", MergeCommandAction);
                _trelloCommand.Register<UptimeCommand, CommentEventArgs>("uptime", UptimeCommandAction);

                using (_trelloService = new TrelloService(_trelloOptions, TimelineEnviroment.Instance))
                using (_gitlabService = new GitLabService(_gitlabOptions, TimelineEnviroment.Instance))
                using (_redmineService = new RedmineService(_redmineOptions, TimelineEnviroment.Instance))
                {
                    _redmineService.UpdateStatuses += OnRedmineService_UpdateStatuses;
                    _redmineService.UpdateIssues += OnRedmineService_UpdateIssues;
                    _redmineService.UpdateProjects += OnRedmine_UpdateProjects;

                    _trelloService.UpdateComments += OnTrelloService_UpdateComments;
                    _trelloService.UpdateStatus += OnTrelloService_UpdateStatus;

                    _gitlabService.MergeRequestsNotify += OnGitlabService_UpdateRequests;

                    _trelloService.Start();
                    _trelloService.Enqueue(CreateBoardTask());

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
                JsonConfig.Write(_card2IssueMapper, CARD_MAPPER_FILE).Wait();
                JsonConfig.Write(_list2StatusMapper, LIST_MAPPER_FILE).Wait();
                JsonConfig.Write(_label2ProjectMapper, LABEL_MAPPER_FILE).Wait();
                JsonConfig.Write(_field2FieldMapper, FIELD_MAPPER_FILE).Wait();

                JsonConfig.Write(_trelloOptions, TRELLO_OPTIONS_FILE).Wait();
                JsonConfig.Write(_gitlabOptions, GITLAB_OPTIONS_FILE).Wait();
                JsonConfig.Write(_redmineOptions, REDMINE_OPTIONS_FILE).Wait();
            }
        }


        static void MergeCommandAction(MergeCommand command, CommentEventArgs args)
        {
            // TODO: Get project id for GitLab MR.
            //_gitlabService.Enqueue(
            //    new UpdateMergeRequestTask(
            //        0,//:???
            //        command.Source,
            //        command.Target,
            //        command.Title,
            //        result => OnCallbackCommandTask(args.CardId, args.CommentId, result)));
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
                    result =>
                    {
                        OnCallbackCommandTask(args.CardId, args.CommentId, result);

                        if (!_field2FieldMapper.TryGetValue(TrelloCustomField.WorkTime, out var fieldId))
                        {
                            _trelloService.Enqueue(CreateCustomField(TrelloCustomField.WorkTime, CustomFieldType.Number));
                            return;
                        }

                        if (!result)
                        {
                            return;
                        }

                        _trelloService.Enqueue(new UpdateCardFieldTask(fieldId: fieldId, cardId: args.CardId, value: command.Hours));
                    }));
        }

        static void OnCallbackCommandTask(string cardId, string commendId, bool result)
        {
            _trelloService.Enqueue(new EmojiCommentTask(
                cardId: cardId,
                commentId: commendId,
                emoji: result ? TrelloService.Success : TrelloService.Failed));
        }

        #region Trello

        static void OnTrelloService_UpdateStatus(object sender, ListEventArgs args)
        {
            if (!_card2IssueMapper.ContainsKey(args.CardId) ||
                !_list2StatusMapper.ContainsKey(args.CurrListId))
                return;

            _redmineService.Enqueue(new UpdateIssueStatusTask(
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
                    // TODO Move to trelloservice propeties.
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
                    subject: $"[{issue.Id}] {issue.Subject}{(issue.EstimatedHours.HasValue && issue.SpentHours.HasValue ? $" - {issue.EstimatedHours}/{issue.SpentHours}" : null)}",
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
                    boardId: _trelloOptions.BoardId,
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

        static void OnRedmine_UpdateProjects(object sender, Project[] projects)
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

        static void OnGitlabService_UpdateRequests(object sender, MergeRequestNotifyEvent[] mergeRequests)
        {
            if (!_field2FieldMapper.TryGetValue(TrelloCustomField.MergeRequest, out var fieldId))
            {
                _trelloService.Enqueue(CreateCustomField(TrelloCustomField.MergeRequest, CustomFieldType.Text));
                return;
            }

            foreach (var request in mergeRequests)
            {
                Match match = Regex.Match(request.Title, "\\[refs #([0-9]+)\\]");
                if (!match.Success || match.Groups.Count < 2 ||
                    !int.TryParse(match.Groups[1].Value, out int issueId) ||
                    !_card2IssueMapper.TryGetValue(issueId, out string cardId))
                    break;

                _trelloService.Enqueue(new UpdateCardFieldTask(fieldId: fieldId, cardId: cardId, value: request.Url));
                // TODO: Продумать как лучше назначать "следующий" статус.
                _redmineService.Enqueue(new UpdateIssueStatusTask(issueId, -1));

                request.Handle = true;
            }
        }

        #endregion Gitlab

        private static TaskItem<ITrelloService, string> CreateCustomField(TrelloCustomField field, CustomFieldType type) 
        {
            return new UpdateFieldTask(
                boardId: _trelloOptions.BoardId,
                name: field.ToString(),
                type: type,
                id: _field2FieldMapper.TryGetValue(field, out string fieldId) ? fieldId : null,
                callback: fieldId =>
                {
                    // TODO: Add repeat if not success.
                    if (string.IsNullOrWhiteSpace(fieldId))
                        return;

                    _field2FieldMapper.TryAdd(fieldId, field);
                });
        }

        private static ITaskItem<ITrelloService> CreateBoardTask()
        {
            var createBoardTask = new UpdateBoardTask(
                id: _trelloOptions.BoardId,
                name: _trelloOptions.BoardName,
                clear: id => id != _trelloOptions.BoardId,
                callback: boardId =>
                {
                    _trelloOptions.BoardId = boardId;

                    _redmineService.Start();
                    _gitlabService.Start();
                });

            createBoardTask
                .Then(CreateCustomField(TrelloCustomField.WorkTime, CustomFieldType.Number))
                .Then(CreateCustomField(TrelloCustomField.MergeRequest, CustomFieldType.Text));

            return createBoardTask;
        }
    }
}
