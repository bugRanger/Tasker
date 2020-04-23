namespace TrelloIntegration.Services.Trello
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using TrelloIntegration.Common.Tasks;
    using TrelloIntegration.Services.Trello.Tasks;

    using Manatee.Trello;

    class TrelloService : ITaskService, IDisposable
    {
        #region Fields

        private IMe _user;
        private ITrelloOptions _options;
        private TrelloFactory _factory;
        private Dictionary<string, IBoard> _boards;
        private Dictionary<string, ICard> _cards;
        private Dictionary<string, IList> _lists;
        private ITaskQueue<TrelloService> _queue;
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
            _cards = new Dictionary<string, ICard>();
            _lists = new Dictionary<string, IList>();

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _queue = new TaskQueue<TrelloService>(task => task.Handle(this));
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
            TrelloConfiguration.EnableConsistencyProcessing = true;
            TrelloConfiguration.HttpClientFactory = () =>
            {
                return
                    new HttpClient(
                        new HttpClientHandler
                        {
                            DefaultProxyCredentials = CredentialCache.DefaultCredentials
                        });
            };

            List.DownloadedFields =
                List.Fields.Name |
                List.Fields.IsClosed |
                List.Fields.Position |
                List.Fields.Board;

            Card.DownloadedFields =
                Card.Fields.Name |
                Card.Fields.Position |
                Card.Fields.Description |
                Card.Fields.List |
                Card.Fields.Comments;

            _queue.Start();

            Enqueue(new SyncCardsTask(_options.Sync));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue<TResult>(TaskItem<TrelloService, TResult> task)
        {
            _queue.Enqueue(task);
        }

        public string Handle(UpdateBoardTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Id) || 
                !_boards.TryGetValue(task.Id, out IBoard board))
            {
                User.Boards.Refresh(ct: _cancellationSource.Token).Wait();
                board = User.Boards.FirstOrDefault(f => f.Id == task.Id);
                if (board == null)
                {
                    board = User.Boards.Add(task.Name, task.Description, ct: _cancellationSource.Token).Result;

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

            if (!string.IsNullOrWhiteSpace(task.Description) && card.Description != task.Description)
                card.Description = task.Description;

            _cards[card.Id] = card;
            return card.Id;
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
 
        public bool Handle(SyncCardsTask task)
        {
            try
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

                    if (card.Comments.Count() != commentCount &&
                        commentCount < card.Comments.Count())
                        for (int i = 0; i < card.Comments.Count() - commentCount; i++)
                            UpdateComments?.Invoke(this,
                                new CommentEventArgs(
                                    card.Id,
                                    card.Comments[i].Id,
                                    card.Comments[i].Creator.Id,
                                    card.Comments[i].Data.Text));
                }

                return true;
            }
            finally
            {
                if (_queue.HasEnabled())
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(_options.Sync.Interval);
                        Enqueue(new SyncCardsTask(_options.Sync));
                    });
            }
        }

        #endregion Methods
    }
}
