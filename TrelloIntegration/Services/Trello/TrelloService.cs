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
        private Dictionary<string, ICard> _cards;
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

        public string Mention => User.Mention;

        #endregion Properties

        #region Events

        public event EventHandler<StatusEventArgs> UpdateStatus;
        public event EventHandler<CommentEventArgs> UpdateComments;
        public event EventHandler<string> Error;

        #endregion Events

        #region Constructors

        public TrelloService(ITrelloOptions options)
        {
            _cards = new Dictionary<string, ICard>();
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

            Card.DownloadedFields =
                Card.Fields.Name |
                Card.Fields.Position |
                Card.Fields.Description |
                Card.Fields.List |
                Card.Fields.Actions |
                Card.Fields.Comments;

            _queue.Start();

            Enqueue(new SyncListTask(_options.Sync));
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

        public string Handle(ImportCardTask task)
        {
            User.Boards.Refresh(ct: _cancellationSource.Token).Wait();

            IBoard board = User.Boards.FirstOrDefault(a => a.Name == task.Project);
            if (board == null)
            {
                board = User.Boards.Add(task.Project, ct: _cancellationSource.Token).Result;
                Handle(new UpdateListTask(board.Id, task.Statuses));
            }

            board.Refresh(ct: _cancellationSource.Token).Wait();

            board.Lists.Refresh(ct: _cancellationSource.Token).Wait();
            IList list =
                board.Lists.FirstOrDefault(a => a.Name == task.Status) ??
                board.Lists.Add(task.Status, ct: _cancellationSource.Token).Result;

            board.Cards.Refresh(ct: _cancellationSource.Token).Wait();
            ICard card = board.Cards.FirstOrDefault(a => a.Name == task.Subject);

            if (card == null)
            {
                card = list.Cards.Add(task.Subject, ct: _cancellationSource.Token).Result;
            }
            else if (card.List.Id != list.Id)
            {
                card.List = list;
            }
            card.Description = task.Description;

            _cards[card.Id] = card;
            return card.Id;
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

        public bool Handle(UpdateListTask task)
        {
            IBoard board = _factory.Board(task.BoardId);
            board.Lists.Refresh(ct: _cancellationSource.Token).Wait();

            foreach (IList item in board.Lists)
            {
                if (!task.Lists.Contains(item.Name))
                    item.IsArchived = true;
            }

            foreach (string list in task.Lists.Reverse())
            {
                if (board.Lists.All(a => a.Name == list))
                    continue;

                board.Lists.Add(list, ct: _cancellationSource.Token);
            }

            return true;
        }

        public bool Handle(SyncListTask task)
        {
            try
            {
                foreach (ICard card in _cards.Values)
                {
                    // Not use action refresh, it is memory leak.
                    string listId = card.List.Id;
                    string listName = card.List.Name;
                    int commentCount = card.Comments.Count();

                    card.Refresh(ct: _cancellationSource.Token).Wait();

                    if (card.List.Id != listId)
                        UpdateStatus?.Invoke(this, new StatusEventArgs(card.Id, listName, card.List.Name));

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
                        Enqueue(new SyncListTask(_options.Sync));
                    });
            }
        }

        #endregion Methods
    }
}
