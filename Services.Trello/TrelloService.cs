namespace Services.Trello
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using NLog;

    using Framework.Timeline;

    using Manatee.Trello;

    using Tasker.Interfaces.Task;
    using Tasker.Common.Task;

    public class TrelloService : ITaskService, ITaskVisitor, IDisposable
    {
        #region Constants

        public static Emoji Success = Emojis.WhiteCheckMark;

        public static Emoji Failed = Emojis.FaceWithSymbolsOnMouth;

        #endregion Constants

        #region Fields

        private ILogger _logger;
        private IMe _user;
        private TrelloFactory _factory;
        private Dictionary<string, IBoard> _boards;
        private Dictionary<string, ICard> _cards;
        private Dictionary<string, IList> _lists;
        private ITaskQueue _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Properties

        public int Id { get; }

        protected IMe User
        {
            get
            {
                _user = _user ?? _factory.Me(ct: _cancellationSource.Token).Result;
                return _user;
            }
        }

        public string Mention => User?.Mention ?? null;

        public ITrelloOptions Options { get; }

        #endregion Properties

        #region Events

        public event Action<object, ITaskCommon> Notify;

        #endregion Events

        #region Constructors

        public TrelloService(int id, ITrelloOptions options, ITimelineEnvironment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            Id = id;
            Options = options;

            _boards = new Dictionary<string, IBoard>();
            _lists = new Dictionary<string, IList>();
            _cards = new Dictionary<string, ICard>();

            _cancellationSource = new CancellationTokenSource();
            _queue = new TaskQueue(task => task.Handle(this), timeline);
            _queue.Error += (task, error) => _logger?.Error($"failed task: {task.GetType()}, error: `{error}`");
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

            TrelloAuthorization.Default.AppKey = Options.AppKey;
            TrelloAuthorization.Default.UserToken = Options.Token;

            _factory ??= new TrelloFactory();

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

            Manatee.Trello.Action.DownloadedFields |= Manatee.Trello.Action.Fields.Reactions;

            Manatee.Trello.List.DownloadedFields =
                Manatee.Trello.List.Fields.Board |
                Manatee.Trello.List.Fields.Name |
                Manatee.Trello.List.Fields.IsClosed |
                Manatee.Trello.List.Fields.Position;

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

            Enqueue(new SyncActionTask(SyncBoardCards, _queue, Options.Sync.Interval));
        }

        public void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(ITaskItem task)
        {
            _queue.Enqueue(task);
        }

        public string Handle(IUpdateTask task)
        {
            // TODO Как мы будем разводтиь понятие ProjectIssue/Board/ProjectMR?
            if (string.IsNullOrWhiteSpace(Options.BoardId) ||
                !_boards.TryGetValue(Options.BoardId, out IBoard board))
            {
                User.Boards.Refresh(ct: _cancellationSource.Token).Wait();

                board =
                    User.Boards.FirstOrDefault(f => f.Id == Options.BoardId) ??
                    User.Boards.Add(Options.BoardName, ct: _cancellationSource.Token).Result;

                // TODO: Добавить обработку/запись исключений для каждой задачи.
                Task.Factory
                    .ContinueWhenAll(new[]
                    {
                        board.CustomFields.Refresh(ct: _cancellationSource.Token),
                        board.Labels.Refresh(ct: _cancellationSource.Token),
                        board.Lists.Refresh(ct: _cancellationSource.Token),
                        board.Cards.Refresh(ct: _cancellationSource.Token),
                    }, s => { })
                    .Wait();

                foreach (IList item in board.Lists)
                    item.IsArchived = true;

                _boards[board.Id] = board;
            }

            if (string.IsNullOrWhiteSpace(task.Context.Status) ||
                !_lists.TryGetValue(task.Context.Status, out IList list))
            {
                list =
                    board.Lists.FirstOrDefault(f => f.Name == task.Context.Status) ??
                    board.Lists.Add(task.Context.Status, ct: _cancellationSource.Token).Result;

                _lists[list.Id] = list;
            }

            if (string.IsNullOrWhiteSpace(task.Id) ||
                !_cards.TryGetValue(task.Id, out ICard card))
            {
                card =
                    board.Cards.FirstOrDefault(f => f.Id == task.Id) ??
                    list.Cards.Add(task.Context.Name, ct: _cancellationSource.Token).Result;

                card.Updated += OnCardUpdated;
            }

            if (card.List?.Id != list.Id)
                card.List = list;

            if (!string.IsNullOrWhiteSpace(task.Context.Name) && card.Name != task.Context.Name)
                card.Name = task.Context.Name;

            if (!string.IsNullOrWhiteSpace(task.Context.Desc) && card.Description != task.Context.Desc)
                card.Description = task.Context.Desc;

            _cards[card.Id] = card;
            return card.Id;
        }

        private bool SyncBoardCards()
        {
            if (_boards.Count == 0)
                return false;

            Task.Factory.ContinueWhenAll(_boards.Values.Select(s => s.Cards.Refresh(ct: _cancellationSource.Token)).ToArray(), s => { }).Wait();
            return true;
        }

        private void OnCardUpdated(ICard card, IEnumerable<string> fields)
        {
            Notify?.Invoke(this, new TaskCommon { Id = card.Id, Context = new TaskContext { Name = card.Name, Desc = card.Description, Status = card.List.Name } });
        }

        #endregion Methods
    }
}
