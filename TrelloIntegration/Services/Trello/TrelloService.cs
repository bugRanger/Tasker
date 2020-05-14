namespace TrelloIntegration.Services.Trello
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Linq;
    using System.Threading;
    using System.Collections.Generic;

    using TrelloIntegration.Common.Tasks;
    using TrelloIntegration.Services.Trello.Tasks;

    using Manatee.Trello;

    class TrelloService : ITrelloVisitor, ITaskService, IDisposable
    {
        #region Fields

        private IMe _user;
        private ITrelloOptions _options;
        private TrelloFactory _factory;
        private Dictionary<string, IBoard> _boards;
        private Dictionary<string, ICard> _cards;
        private Dictionary<string, IList> _lists;
        private Dictionary<string, ILabel> _labels;
        private Dictionary<string, ICustomFieldDefinition> _fields;
        private ITaskQueue<ITrelloVisitor> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Properties

        protected IMe User
        {
            get
            {
                _user = _user ?? _factory.Me(ct: _cancellationSource.Token).Result;
                return _user;
            }
        }

        public string Mention => User?.Mention ?? null;

        public static Emoji Success = Emojis.WhiteCheckMark;

        public static Emoji Failed = Emojis.FaceWithSymbolsOnMouth;

        #endregion Properties

        #region Events

        public event EventHandler<ListEventArgs> UpdateStatus;
        public event EventHandler<CommentEventArgs> UpdateComments;
        public event EventHandler<string> Error;

        #endregion Events

        #region Constructors

        public TrelloService(ITrelloOptions options)
        {
            _boards = new Dictionary<string, IBoard>();
            _fields = new Dictionary<string, ICustomFieldDefinition>();
            _labels = new Dictionary<string, ILabel>();
            _lists = new Dictionary<string, IList>();
            _cards = new Dictionary<string, ICard>();

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<ITrelloVisitor>(task => task.Handle(this));
            _queue.Error += (sender, error) => Error?.Invoke(this, error);
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            if (_queue.HasEnabled())
                return;

            TrelloAuthorization.Default.AppKey = _options.AppKey;
            TrelloAuthorization.Default.UserToken = _options.Token;

            _factory = _factory ?? new TrelloFactory();

            TrelloConfiguration.EnableDeepDownloads = false;
            TrelloConfiguration.EnableConsistencyProcessing = false;
            TrelloConfiguration.HttpClientFactory = () =>
            {
                return
                    new HttpClient(
                        new HttpClientHandler
                        {
                            DefaultProxyCredentials = CredentialCache.DefaultCredentials
                        });
            };

            Manatee.Trello.Action.DownloadedFields |=
                Manatee.Trello.Action.Fields.Reactions;

            List.DownloadedFields =
                List.Fields.Board |
                List.Fields.Name |
                List.Fields.IsClosed |
                List.Fields.Position;

            Card.DownloadedFields =
                Card.Fields.List |
                Card.Fields.Labels |
                Card.Fields.Name |
                Card.Fields.Position |
                Card.Fields.Description |
                Card.Fields.CustomFields |
                Card.Fields.Comments;

            _queue.Start();

            Enqueue(new SyncActionTask<ITrelloVisitor>(SyncCards, _queue, _options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem<ITrelloVisitor> task)
        {
            _queue.Enqueue(task);
        }

        public string Handle(UpdateBoardTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Id) || 
                !_boards.TryGetValue(task.Id, out IBoard board))
            {
                User.Boards.Refresh(ct: _cancellationSource.Token).Wait();

                board = 
                    User.Boards.FirstOrDefault(f => f.Id == task.Id) ??
                    User.Boards.Add(task.Name, task.Description, ct: _cancellationSource.Token).Result;

                if (task.СlearСontents?.Invoke(board.Id) == true)
                {
                    board.Lists.Refresh(ct: _cancellationSource.Token).Wait();
                    foreach (IList item in board.Lists)
                        item.IsArchived = true;
                }
            }

            if (board.Name != task.Name)
                board.Name = task.Name;

            if (board.Description != task.Description)
                board.Description = task.Description;

            _boards[board.Id] = board;
            return board.Id;
        }

        public string Handle(UpdateFieldTask task)
        {
            if (string.IsNullOrWhiteSpace(task.BoardId) ||
                !_boards.TryGetValue(task.BoardId, out IBoard board))
                return null;

            if (string.IsNullOrWhiteSpace(task.Id) ||
                !_fields.TryGetValue(task.Id, out ICustomFieldDefinition field))
            {
                board.CustomFields.Refresh(ct: _cancellationSource.Token).Wait();
                field =
                    board.CustomFields.FirstOrDefault(f => f.Id == task.Id) ??
                    board.CustomFields.Add(task.Name, task.Type, options: task.Options, ct: _cancellationSource.Token).Result;
            }

            if (!string.IsNullOrWhiteSpace(field.Name) && field.Name != task.Name)
                field.Name = task.Name;

            _fields[field.Id] = field;
            return field.Id;
        }

        public string Handle(UpdateLabelTask task)
        {
            if (string.IsNullOrWhiteSpace(task.BoardId) ||
                !_boards.TryGetValue(task.BoardId, out IBoard board))
                return null;

            if (string.IsNullOrWhiteSpace(task.Id) ||
                !_labels.TryGetValue(task.Id, out ILabel item))
            {
                board.Labels.Refresh(ct: _cancellationSource.Token).Wait();
                item =
                    board.Labels.FirstOrDefault(f => f.Id == task.Id) ??
                    board.Labels.Add(task.Name, task.Color, ct: _cancellationSource.Token).Result;
            }

            _labels[item.Id] = item;
            return item.Id;
        }

        public string Handle(UpdateListTask task)
        {
            if (string.IsNullOrWhiteSpace(task.BoardId) || 
                !_boards.TryGetValue(task.BoardId, out IBoard board))
                return null;

            if (string.IsNullOrWhiteSpace(task.ListId) || 
                !_lists.TryGetValue(task.ListId, out IList list))
            {
                board.Lists.Refresh(ct: _cancellationSource.Token).Wait();
                list = 
                    board.Lists.FirstOrDefault(f => f.Id == task.ListId) ??
                    board.Lists.Add(task.Name, ct: _cancellationSource.Token).Result;
            }

            _lists[list.Id] = list;
            return list.Id;
        }

        public string Handle(UpdateCardTask task)
        {
            if (string.IsNullOrWhiteSpace(task.BoardId) ||
                !_boards.TryGetValue(task.BoardId, out IBoard board))
                return null;

            if (string.IsNullOrWhiteSpace(task.ListId) || 
                !_lists.TryGetValue(task.ListId, out IList list))
                return null;

            if (string.IsNullOrWhiteSpace(task.CardId) || 
                !_cards.TryGetValue(task.CardId, out ICard card))
            {
                board.Cards.Refresh(ct: _cancellationSource.Token).Wait();
                card = 
                    board.Cards.FirstOrDefault(f => f.Id == task.CardId) ??
                    list.Cards.Add(task.Subject, ct: _cancellationSource.Token).Result;
            }

            if (card.List.Id != task.ListId)
                card.List = _lists[task.ListId];

            if (!string.IsNullOrWhiteSpace(card.Name) && card.Name != task.Subject)
                card.Name = task.Subject;

            if (!string.IsNullOrWhiteSpace(task.Description) && card.Description != task.Description)
                card.Description = task.Description;

            if (!string.IsNullOrWhiteSpace(task.LabelId) && _labels.TryGetValue(task.LabelId, out ILabel label))
            {
                card.Labels.Refresh(ct: _cancellationSource.Token).Wait();
                if (card.Labels.FirstOrDefault(f => f.Id == label.Id) == null)
                    card.Labels.Add(label, ct: _cancellationSource.Token).Wait();
            }

            _cards[card.Id] = card;
            return card.Id;
        }

        public bool Handle(UpdateCardFieldTask task)
        {
            if (string.IsNullOrWhiteSpace(task.FieldId) || !_fields.TryGetValue(task.FieldId, out ICustomFieldDefinition field) ||
                string.IsNullOrWhiteSpace(task.CardId) || !_cards.TryGetValue(task.CardId, out ICard card))
                return false;

            switch (field.Type)
            {
                case CustomFieldType.CheckBox:
                    field.SetValueForCard(card, task.Value != null ? (bool?)Convert.ChangeType(task.Value, typeof(bool)) : null, _cancellationSource.Token).Wait();
                    break;

                case CustomFieldType.Number:
                    field.SetValueForCard(card, task.Value != null ? (double?)Convert.ChangeType(task.Value, typeof(double)) : null, _cancellationSource.Token).Wait();
                    break;

                case CustomFieldType.Text:
                    field.SetValueForCard(card, (string)Convert.ChangeType(task.Value, typeof(string)), _cancellationSource.Token).Wait();
                    break;

                case CustomFieldType.DateTime:
                    field.SetValueForCard(card, task.Value != null ? (DateTime?)Convert.ChangeType(task.Value, typeof(DateTime)) : null, _cancellationSource.Token).Wait();
                    break;

                default:
                    return false;
            }

            return true;
        }

        public bool Handle(AddCommentTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Comment) ||
                !_cards.TryGetValue(task.CardId, out ICard card))
                return false;

            card.Comments.Add(task.Comment, _cancellationSource.Token).Wait();

            return true;
        }

        public bool Handle(EmojiCommentTask task)
        {
            if (task.Emoji == null || !_cards.TryGetValue(task.CardId, out ICard card))
                return false;

            var comment = card.Comments.FirstOrDefault(f => f.Id == task.CommentId);
            if (comment == null)
                return false;

            comment.Reactions.Add(task.Emoji, _cancellationSource.Token).Wait();

            return true;
        }

        public bool SyncCards()
        {
            foreach (ICard card in _cards.Values)
            {
                // Not use action refresh, it is memory leak.
                string listId = card.List.Id;
                int commentCount = card.Comments.Count();

                card.Refresh(ct: _cancellationSource.Token).Wait();

                if (card.List.Id != listId)
                    UpdateStatus?.Invoke(this, new ListEventArgs(
                        cardId: card.Id, 
                        prevId: listId, 
                        currId: card.List.Id));

                // TODO Add notification for upgrade cards.
                //card.CustomFields.Refresh(ct: _cancellationSource.Token).Wait();
                //foreach (var item in card.CustomFields)
                //{
                //    item.Refresh();
                //    item.Definition.Refresh();
                //    item.Definition.Options.Refresh();
                //    // TODO Add support new value.
                //    item.Definition.SetValueForCard
                //}

                card.Comments.Refresh(ct: _cancellationSource.Token).Wait();
                var updateComments = card.Comments.Where(w => w.Reactions
                    .FirstOrDefault(f => 
                        f.Member.Mention == Mention && 
                        f.Emoji.Equals(Success) ||
                        f.Emoji.Equals(Failed)) == null).ToArray();

                foreach (var comment in updateComments)
                {
                    UpdateComments?.Invoke(this,
                        new CommentEventArgs(
                            card.Id,
                            comment.Id,
                            comment.Creator.Id,
                            comment.Data.Text));
                }
            }

            return true;
        }

        #endregion Methods
    }
}
