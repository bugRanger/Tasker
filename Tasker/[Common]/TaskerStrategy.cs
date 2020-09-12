namespace Tasker
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NLog;

    using Common.Tasks;
    using Common.Command;

    using Manatee.Trello;

    using Services.Trello;
    using Services.Trello.Tasks;
    using Services.Trello.Commands;

    using Services.GitLab;
    using Services.GitLab.Tasks;

    using Services.Redmine;
    using Services.Redmine.Tasks;

    using TrelloCustomField = Services.Trello.CustomField;
    using Framework.Common;

    // TODO: Разделить отвественность по сервисам.
    public class TaskerStrategy : ITaskerStrategy,  ITrelloBehaviors, IRedmineBehaviors, IGitLabBehaviors
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly Locker _locker;
        private readonly ICommandController _trelloCommand;

        private ITaskerService _service;

        #endregion Fields

        #region Constructor

        public TaskerStrategy(IServiceMapper mapper)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _locker = new Locker();

            _trelloCommand = new CommandController(() => $"^{_service.TrelloService.Mention} ([A-Za-z]+):");
            _trelloCommand.Register<MergeCommand, CardComment>("merge", MergeCommandAction);
            _trelloCommand.Register<UptimeCommand, CardComment>("uptime", UptimeCommandAction);
        }

        #endregion Constructor

        #region Methods

        public void Start(ITaskerService service)
        {
            if (!_locker.SetEnabled())
                return;

            _service = service;
            _service.TrelloService.Register(this);
            _service.GitLabService.Register(this);
            _service.RedmineService.Register(this);

            _service.TrelloService.Start();
            _service.TrelloService.Enqueue(CreateBoardTask());
        }

        public void Stop() 
        {
            if (!_locker.SetDisabled())
                return;

            _service.TrelloService.Stop();
            _service.GitLabService.Stop();
            _service.RedmineService.Stop();
        }

        private void MergeCommandAction(MergeCommand command, CardComment args)
        {
            // TODO: Get project id for GitLab MR.
            //_service.GitLabService.Enqueue(
            //    new UpdateMergeRequestTask(
            //        0,//:???
            //        command.Source,
            //        command.Target,
            //        command.Title,
            //        result => OnCallbackCommandTask(args.CardId, args.CommentId, result)));
        }

        private void UptimeCommandAction(UptimeCommand command, CardComment args)
        {
            if (!_service.Mapper.Card2IssueMapper.TryGetValue(args.CardId, out int issueId))
                return;

            _service.RedmineService.Enqueue(
                new UpdateWorkTimeTask(
                    issueId,
                    command.Hours,
                    command.Comment,
                    result =>
                    {
                        OnCallbackCommandTask(args.CardId, args.CommentId, result);

                        if (!_service.Mapper.Field2FieldMapper.TryGetValue(TrelloCustomField.WorkTime, out var fieldId))
                        {
                            _service.TrelloService.Enqueue(CreateCustomField(TrelloCustomField.WorkTime, CustomFieldType.Number));
                            return;
                        }

                        if (!result)
                        {
                            return;
                        }

                        _service.TrelloService.Enqueue(new UpdateCardFieldTask(fieldId: fieldId, cardId: args.CardId, value: command.Hours));
                    }));
        }

        private TaskItem<ITrelloVisitor, string> CreateCustomField(TrelloCustomField field, CustomFieldType type)
        {
            return new UpdateFieldTask(
                boardId: _service.TrelloService.Options.BoardId,
                name: field.ToString(),
                type: type,
                id: _service.Mapper.Field2FieldMapper.TryGetValue(field, out string fieldId) ? fieldId : null,
                callback: fieldId =>
                {
                    // TODO: Add repeat if not success.
                    if (string.IsNullOrWhiteSpace(fieldId) || _service.Mapper.Field2FieldMapper.ContainsKey(fieldId))
                        return;

                    _service.Mapper.Field2FieldMapper[fieldId] = field;
                });
        }

        private ITaskItem<ITrelloVisitor> CreateBoardTask()
        {
            var createBoardTask = new UpdateBoardTask(
                id: _service.TrelloService.Options.BoardId,
                name: _service.TrelloService.Options.BoardName,
                clear: id => id != _service.TrelloService.Options.BoardId,
                callback: boardId =>
                {
                    _service.TrelloService.Options.BoardId = boardId;

                    _service.RedmineService.Start();
                    _service.GitLabService.Start();
                });

            createBoardTask
                .Then(CreateCustomField(TrelloCustomField.WorkTime, CustomFieldType.Number))
                .Then(CreateCustomField(TrelloCustomField.MergeRequest, CustomFieldType.Text));

            return createBoardTask;
        }

        private void OnCallbackCommandTask(string cardId, string commendId, bool result)
        {
            _service.TrelloService.Enqueue(new EmojiCommentTask(
                cardId: cardId,
                commentId: commendId,
                emoji: result ? TrelloService.Success : TrelloService.Failed));
        }

        #endregion Methods

        #region Trello

        public void UpdateComment(CardComment comment)
        {
            if (!_service.Mapper.Card2IssueMapper.ContainsKey(comment.CardId))
                return;

            if (_trelloCommand.TryParse(comment.Text, out var command))
            {
                bool result = _trelloCommand.TryAction(command, comment);

                _service.TrelloService.Enqueue(new EmojiCommentTask(
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
            if (!_service.Mapper.Card2IssueMapper.ContainsKey(board.CardId) ||
                !_service.Mapper.List2StatusMapper.ContainsKey(board.CurrListId))
                return;

            _service.RedmineService.Enqueue(new UpdateIssueStatusTask(
                issueId: _service.Mapper.Card2IssueMapper[board.CardId],
                statusId: _service.Mapper.List2StatusMapper[board.CurrListId],
                callback: result =>
                {
                    if (!result)
                        _service.TrelloService.Enqueue(new UpdateCardTask(boardId: _service.TrelloService.Options.BoardId, () => board.CardId, () => board.PrevListId));
                }));
        }

        #endregion Trello

        #region Redmine

        public void UpdateIssues(Issue[] issues)
        {
            // Update cards.
            foreach (var issue in issues)
            {
                _service.TrelloService.Enqueue(new UpdateCardTask(
                    boardId: _service.TrelloService.Options.BoardId,
                    subject: $"[{issue.Id}] {issue.Subject}{(issue.EstimatedHours.HasValue && issue.SpentHours.HasValue ? $" - {issue.EstimatedHours}/{issue.SpentHours}" : null)}",
                    description: issue.Description,
                    getCardId: () => _service.Mapper.Card2IssueMapper.TryGetValue(issue.Id, out string cardId) ? cardId : null,
                    getListId: () => _service.Mapper.List2StatusMapper.TryGetValue(issue.Status.Id, out string listId) ? listId : null,
                    getLabelId: () => _service.Mapper.Label2ProjectMapper.TryGetValue(issue.Project.Id, out string labelId) ? labelId : null,
                    callback: cardId =>
                    {
                        if (string.IsNullOrWhiteSpace(cardId))
                            return;

                        _service.Mapper.Card2IssueMapper.Add(cardId, issue.Id);
                    }));

                // TODO: Добавить создание МРа если задача оказалась на ревью.
                //_service.GitLabService.Enqueue(new UpdateMergeRequestTask(_service.GitLabService.Options.ProjectId, "", ""));
            }
        }

        public void UpdateProjects(Project[] projects)
        {
            foreach (var project in projects)
            {
                string labelId = _service.Mapper.Label2ProjectMapper.TryGetValue(project.Id, out string label) ? label : null;
                _service.TrelloService.Enqueue(new UpdateLabelTask(
                    boardId: _service.TrelloService.Options.BoardId,
                    id: labelId,
                    name: project.Name,
                    callback: labelId =>
                    {
                        if (string.IsNullOrWhiteSpace(labelId))
                            return;

                        _service.Mapper.Label2ProjectMapper.Add(labelId, project.Id);
                    }));
            }
        }

        public void UpdateStatuses(IssueStatus[] statuses)
        {
            if (_service.RedmineService.Options.Statuses == null)
                return;

            var dict = statuses.ToDictionary(k => k.Id, v => v);

            foreach (int statusId in _service.RedmineService.Options.Statuses.Reverse())
            {
                if (!dict.TryGetValue(statusId, out var issueStatus))
                    continue;

                string listId = _service.Mapper.List2StatusMapper.TryGetValue(statusId, out string list) ? list : null;
                //var position;
                _service.TrelloService.Enqueue(new UpdateListTask(
                    boardId: _service.TrelloService.Options.BoardId,
                    listId: listId,
                    name: issueStatus.Name,
                    callback: listId =>
                    {
                        if (string.IsNullOrWhiteSpace(listId))
                            return;

                        _service.Mapper.List2StatusMapper.Add(listId, statusId);
                    }));
            }
        }

        #endregion Redmine

        #region GitLab

        public void UpdateMerges(MergeRequest[] mergeRequests)
        {
            if (!_service.Mapper.Field2FieldMapper.TryGetValue(TrelloCustomField.MergeRequest, out var fieldId))
            {
                _service.TrelloService.Enqueue(CreateCustomField(TrelloCustomField.MergeRequest, CustomFieldType.Text));
                return;
            }

            foreach (var request in mergeRequests)
            {
                // TODO: Избавиться от регулярного выражения.
                Match match = Regex.Match(request.Title, "\\[refs #([0-9]+)\\]");
                if (!match.Success || match.Groups.Count < 2 ||
                    !int.TryParse(match.Groups[1].Value, out int issueId) ||
                    !_service.Mapper.Card2IssueMapper.TryGetValue(issueId, out string cardId))
                    break;

                _service.TrelloService.Enqueue(new UpdateCardFieldTask(fieldId: fieldId, cardId: cardId, value: request.Url));
                // TODO: Перевод задачи с ревью на новый статус после вливания МРа.
                _service.RedmineService.Enqueue(new UpdateIssueStatusTask(issueId, -1));
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
