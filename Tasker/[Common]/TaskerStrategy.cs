namespace Tasker
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NLog;

    using Common.Tasks;
    using Common.Command;
    using Framework.Common;

    using Manatee.Trello;

    using Services.Trello;
    using Services.Trello.Tasks;
    using Services.Trello.Commands;

    using Services.GitLab;
    using Services.GitLab.Tasks;

    using Services.Redmine;
    using Services.Redmine.Tasks;

    using TrelloCustomField = Services.Trello.CustomField;

    public class TaskerStrategy : ITrelloStrategy, IRedmineStrategy, IGitLabStrategy
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly ICommandController _trelloCommand;
        private readonly IServiceMapper _mapper;

        private ITrelloService _trelloService;
        private IGitLabService _gitlabService;
        private IRedmineService _redmineService;

        #endregion Fields

        #region Constructor

        public TaskerStrategy(IServiceMapper mapper)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _mapper = mapper;

            _trelloCommand = new CommandController(() => $"^{_trelloService.Mention} ([A-Za-z]+):");
            _trelloCommand.Register<MergeCommand, CardComment>("merge", MergeCommandAction);
            _trelloCommand.Register<UptimeCommand, CardComment>("uptime", UptimeCommandAction);
        }

        ~TaskerStrategy() 
        {
            _trelloService = null;
            _gitlabService = null;
            _redmineService = null;
        }

        #endregion Constructor

        #region Methods

        public void Start()
        {
            _trelloService.Start();
            _trelloService.Enqueue(CreateBoardTask());
        }

        public void Stop() 
        {
            _trelloService.Stop();
            _gitlabService.Stop();
            _redmineService.Stop();
        }

        private void MergeCommandAction(MergeCommand command, CardComment args)
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

        private void UptimeCommandAction(UptimeCommand command, CardComment args)
        {
            if (!_mapper.Card2IssueMapper.TryGetValue(args.CardId, out int issueId))
                return;

            _redmineService.Enqueue(
                new UpdateWorkTimeTask(
                    issueId,
                    command.Hours,
                    command.Comment,
                    result =>
                    {
                        OnCallbackCommandTask(args.CardId, args.CommentId, result);

                        if (!_mapper.Field2FieldMapper.TryGetValue(TrelloCustomField.WorkTime, out var fieldId))
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

        private TaskItem<ITrelloVisitor, string> CreateCustomField(TrelloCustomField field, CustomFieldType type)
        {
            return new UpdateFieldTask(
                boardId: _trelloService.Options.BoardId,
                name: field.ToString(),
                type: type,
                id: _mapper.Field2FieldMapper.TryGetValue(field, out string fieldId) ? fieldId : null,
                callback: fieldId =>
                {
                    // TODO: Add repeat if not success.
                    if (string.IsNullOrWhiteSpace(fieldId) || _mapper.Field2FieldMapper.ContainsKey(fieldId))
                        return;

                    _mapper.Field2FieldMapper[fieldId] = field;
                });
        }

        private ITaskItem<ITrelloVisitor> CreateBoardTask()
        {
            var createBoardTask = new UpdateBoardTask(
                id: _trelloService.Options.BoardId,
                name: _trelloService.Options.BoardName,
                clear: id => id != _trelloService.Options.BoardId,
                callback: boardId =>
                {
                    _trelloService.Options.BoardId = boardId;

                    _redmineService.Start();
                    _gitlabService.Start();
                });

            createBoardTask
                .Then(CreateCustomField(TrelloCustomField.WorkTime, CustomFieldType.Number))
                .Then(CreateCustomField(TrelloCustomField.MergeRequest, CustomFieldType.Text));

            return createBoardTask;
        }

        private void OnCallbackCommandTask(string cardId, string commendId, bool result)
        {
            _trelloService.Enqueue(new EmojiCommentTask(
                cardId: cardId,
                commentId: commendId,
                emoji: result ? TrelloService.Success : TrelloService.Failed));
        }

        #endregion Methods

        #region Trello

        public void Register(ITrelloService service)
        {
            _trelloService = service;
        }

        public void UpdateComment(CardComment comment)
        {
            if (!_mapper.Card2IssueMapper.ContainsKey(comment.CardId))
                return;

            if (_trelloCommand.TryParse(comment.Text, out var command))
            {
                bool result = _trelloCommand.TryAction(command, comment);

                _trelloService.Enqueue(new EmojiCommentTask(
                    cardId: comment.CardId,
                    commentId: comment.CommentId,
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

        public void UpdateList(BoardList board)
        {
            if (!_mapper.Card2IssueMapper.ContainsKey(board.CardId) ||
                !_mapper.List2StatusMapper.ContainsKey(board.CurrListId))
                return;

            _redmineService.Enqueue(new UpdateIssueStatusTask(
                issueId: _mapper.Card2IssueMapper[board.CardId],
                statusId: _mapper.List2StatusMapper[board.CurrListId],
                callback: result =>
                {
                    if (!result)
                        _trelloService.Enqueue(new UpdateCardTask(boardId: _trelloService.Options.BoardId, () => board.CardId, () => board.PrevListId));
                }));
        }

        #endregion Trello

        #region Redmine

        public void Register(IRedmineService service)
        {
            _redmineService = service;
        }

        public void UpdateIssues(Issue[] issues)
        {
            // Update cards.
            foreach (var issue in issues)
            {
                _trelloService.Enqueue(new UpdateCardTask(
                    boardId: _trelloService.Options.BoardId,
                    subject: $"[{issue.Id}] {issue.Subject}{(issue.EstimatedHours.HasValue && issue.SpentHours.HasValue ? $" - {issue.EstimatedHours}/{issue.SpentHours}" : null)}",
                    description: issue.Description,
                    getCardId: () => _mapper.Card2IssueMapper.TryGetValue(issue.Id, out string cardId) ? cardId : null,
                    getListId: () => _mapper.List2StatusMapper.TryGetValue(issue.Status.Id, out string listId) ? listId : null,
                    getLabelId: () => _mapper.Label2ProjectMapper.TryGetValue(issue.Project.Id, out string labelId) ? labelId : null,
                    callback: cardId =>
                    {
                        if (string.IsNullOrWhiteSpace(cardId))
                            return;

                        _mapper.Card2IssueMapper.Add(cardId, issue.Id);
                    }));

                //_gitlabService.Enqueue(new UpdateMergeRequestTask(_gitlabService.Options.ProjectId, "", ""));
            }
        }

        public void UpdateProjects(Project[] projects)
        {
            foreach (var project in projects)
            {
                string labelId = _mapper.Label2ProjectMapper.TryGetValue(project.Id, out string label) ? label : null;
                _trelloService.Enqueue(new UpdateLabelTask(
                    boardId: _trelloService.Options.BoardId,
                    id: labelId,
                    name: project.Name,
                    callback: labelId =>
                    {
                        if (string.IsNullOrWhiteSpace(labelId))
                            return;

                        _mapper.Label2ProjectMapper.Add(labelId, project.Id);
                    }));
            }
        }

        public void UpdateStatuses(IssueStatus[] statuses)
        {
            if (_redmineService.Options.Statuses == null)
                return;

            var dict = statuses.ToDictionary(k => k.Id, v => v);

            foreach (int statusId in _redmineService.Options.Statuses.Reverse())
            {
                if (!dict.TryGetValue(statusId, out var issueStatus))
                    continue;

                string listId = _mapper.List2StatusMapper.TryGetValue(statusId, out string list) ? list : null;
                //var position;
                _trelloService.Enqueue(new UpdateListTask(
                    boardId: _trelloService.Options.BoardId,
                    listId: listId,
                    name: issueStatus.Name,
                    callback: listId =>
                    {
                        if (string.IsNullOrWhiteSpace(listId))
                            return;

                        _mapper.List2StatusMapper.Add(listId, statusId);
                    }));
            }
        }

        #endregion Redmine

        #region GitLab

        public void Register(IGitLabService service)
        {
            _gitlabService = service;
        }

        public void UpdateMerges(MergeRequest[] mergeRequests)
        {
            if (!_mapper.Field2FieldMapper.TryGetValue(TrelloCustomField.MergeRequest, out var fieldId))
            {
                _trelloService.Enqueue(CreateCustomField(TrelloCustomField.MergeRequest, CustomFieldType.Text));
                return;
            }

            foreach (var request in mergeRequests)
            {
                // TODO: Избавиться от регулярного выражения.
                Match match = Regex.Match(request.Title, "\\[refs #([0-9]+)\\]");
                if (!match.Success || match.Groups.Count < 2 ||
                    !int.TryParse(match.Groups[1].Value, out int issueId) ||
                    !_mapper.Card2IssueMapper.TryGetValue(issueId, out string cardId))
                    break;

                _trelloService.Enqueue(new UpdateCardFieldTask(fieldId: fieldId, cardId: cardId, value: request.Url));
                // TODO: Продумать как лучше назначать "следующий" статус.
                _redmineService.Enqueue(new UpdateIssueStatusTask(issueId, -1));
            }
        }

        public void UpdateBranches(Branch[] branches)
        {
            // TODO: Impl.
            throw new NotImplementedException();
        }

        #endregion GitLab
    }
}
