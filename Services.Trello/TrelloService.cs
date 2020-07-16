namespace Services.Trello
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Collections.Generic;

    using Manatee.Trello;

    using Common;
    using Common.Tasks;

    using Services.Trello.Tasks;
    using System.Threading.Tasks;

    public class TrelloService : ITrelloService, IDisposable
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
        private ITaskQueue<ITrelloService> _queue;
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

        public TrelloService(ITrelloOptions options, ITimelineEnviroment timeline)
        {
            _boards = new Dictionary<string, IBoard>();
            _fields = new Dictionary<string, ICustomFieldDefinition>();
            _labels = new Dictionary<string, ILabel>();
            _lists = new Dictionary<string, IList>();
            _cards = new Dictionary<string, ICard>();

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<ITrelloService>(task => task.Handle(this), timeline);
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
                Card.Fields.Actions |
                Card.Fields.List |
                Card.Fields.Labels |
                Card.Fields.Name |
                Card.Fields.Position |
                Card.Fields.Description |
                Card.Fields.CustomFields |
                Card.Fields.Comments;

            _queue.Start();

            Enqueue(new SyncActionTask<ITrelloService>(SyncBoardCards, _queue, _options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem<ITrelloService> task)
        {
            _queue.Enqueue(task);
        }

        public string Handle(IUpdateBoardTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Id) ||
                !_boards.TryGetValue(task.Id, out IBoard board))
            {
                User.Boards.Refresh(ct: _cancellationSource.Token).Wait();

                board =
                    User.Boards.FirstOrDefault(f => f.Id == task.Id) ??
                    User.Boards.Add(task.Name, task.Description, ct: _cancellationSource.Token).Result;
            }

            if (board.Name != task.Name)
                board.Name = task.Name;

            if (board.Description != task.Description)
                board.Description = task.Description;

            // TODO: Добавить обработку/запись исключений через для каждой задачи.
            Task.Factory.ContinueWhenAll(new[]
            {
                board.CustomFields.Refresh(ct: _cancellationSource.Token),
                board.Labels.Refresh(ct: _cancellationSource.Token),
                board.Lists.Refresh(ct: _cancellationSource.Token),
                board.Cards.Refresh(ct: _cancellationSource.Token),
            }, 
            s => { }).Wait();

            if (task.СlearСontents?.Invoke(board.Id) == true)
            {
                foreach (IList item in board.Lists)
                    item.IsArchived = true;
            }

            _boards[board.Id] = board;
            return board.Id;
        }

        public string Handle(IUpdateFieldTask task)
        {
            if (string.IsNullOrWhiteSpace(task.BoardId) ||
                !_boards.TryGetValue(task.BoardId, out IBoard board))
                return null;

            if (string.IsNullOrWhiteSpace(task.Id) ||
                !_fields.TryGetValue(task.Id, out ICustomFieldDefinition field))
            {
                field =
                    board.CustomFields.FirstOrDefault(f => f.Id == task.Id) ?? 
                    board.CustomFields.Add(task.Name, task.Type, options: task.Options, ct: _cancellationSource.Token).Result;
            }

            if (!string.IsNullOrWhiteSpace(field.Name) && field.Name != task.Name)
                field.Name = task.Name;

            _fields[field.Id] = field;
            return field.Id;
        }

        public string Handle(IUpdateLabelTask task)
        {
            if (string.IsNullOrWhiteSpace(task.BoardId) ||
                !_boards.TryGetValue(task.BoardId, out IBoard board))
                return null;

            if (string.IsNullOrWhiteSpace(task.Id) ||
                !_labels.TryGetValue(task.Id, out ILabel item))
            {
                item =
                    board.Labels.FirstOrDefault(f => f.Id == task.Id) ??
                    board.Labels.Add(task.Name, task.Color, ct: _cancellationSource.Token).Result;
            }

            _labels[item.Id] = item;
            return item.Id;
        }

        public string Handle(IUpdateListTask task)
        {
            if (string.IsNullOrWhiteSpace(task.BoardId) || 
                !_boards.TryGetValue(task.BoardId, out IBoard board))
                return null;

            if (string.IsNullOrWhiteSpace(task.ListId) || 
                !_lists.TryGetValue(task.ListId, out IList list))
            {
                list = 
                    board.Lists.FirstOrDefault(f => f.Id == task.ListId) ??
                    board.Lists.Add(task.Name, ct: _cancellationSource.Token).Result;
            }

            _lists[list.Id] = list;
            return list.Id;
        }

        public string Handle(IUpdateCardTask task)
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
                card = 
                    board.Cards.FirstOrDefault(f => f.Id == task.CardId) ??
                    list.Cards.Add(task.Subject, ct: _cancellationSource.Token).Result;

                card.Updated += Card_Updated;
                //_factory.Webhook(card, "https://localhost:44326/api/trello");
            }

            if (card.List?.Id != task.ListId)
                card.List = _lists[task.ListId];

            if (!string.IsNullOrWhiteSpace(task.Subject) && card.Name != task.Subject)
                card.Name = task.Subject;

            if (!string.IsNullOrWhiteSpace(task.Description) && card.Description != task.Description)
                card.Description = task.Description;

            if (!string.IsNullOrWhiteSpace(task.LabelId) && _labels.TryGetValue(task.LabelId, out ILabel label))
            {
                if (card.Labels.FirstOrDefault(f => f.Id == label.Id) == null)
                    card.Labels.Add(label, ct: _cancellationSource.Token).Wait();
            }

            _cards[card.Id] = card;
            return card.Id;
        }

        private void Card_Updated(ICard card, IEnumerable<string> fields)
        {
            foreach (Card.Fields field in fields.Select(s => { return Enum.TryParse(s, out Card.Fields value) ? value : Card.Fields.IsSubscribed; }))
            {
                switch (field)
                {
                    case Card.Fields.List:
                        card.Actions.Refresh(ct: _cancellationSource.Token).Wait();
                        UpdateStatus?.Invoke(this, new ListEventArgs(cardId: card.Id, card.Actions.Last().Data.ListBefore.Id, card.Actions.Last().Data.ListAfter.Id));
                        break;

                    case Card.Fields.Comments:
                        var updateComments = card.Comments.Where(w => w.Reactions
                            .FirstOrDefault(f => f.Member.Mention == Mention && f.Emoji.Equals(Success) || f.Emoji.Equals(Failed)) == null).ToArray();

                        foreach (var comment in updateComments)
                        {
                            UpdateComments?.Invoke(this, new CommentEventArgs(card.Id, comment.Id, comment.Creator.Id, comment.Data.Text));
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public bool Handle(IUpdateCardFieldTask task)
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

        public bool Handle(IAddCommentTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Comment) ||
                !_cards.TryGetValue(task.CardId, out ICard card))
                return false;

            card.Comments.Add(task.Comment, _cancellationSource.Token).Wait();

            return true;
        }

        public bool Handle(IEmojiCommentTask task)
        {
            if (task.Emoji == null || !_cards.TryGetValue(task.CardId, out ICard card))
                return false;

            var comment = card.Comments.FirstOrDefault(f => f.Id == task.CommentId);
            if (comment == null)
                return false;

            comment.Reactions.Add(task.Emoji, _cancellationSource.Token).Wait();

            return true;
        }

        private bool SyncBoardCards()
        {
            if (_boards.Count == 0)
                return false;

            Task.Factory.ContinueWhenAll(_boards.Values.Select(s => s.Cards.Refresh(ct: _cancellationSource.Token)).ToArray(), s => { }).Wait();
            return true;
        }

        #endregion Methods
    }
}
