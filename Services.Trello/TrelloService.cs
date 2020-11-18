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

    using Tasker.Common.Task;
    using Tasker.Interfaces.Task;

    public class TrelloService : ITaskService, ITaskVisitor, IDisposable
    {
        #region Constants

        public static Emoji Success = Emojis.WhiteCheckMark;

        public static Emoji Failed = Emojis.FaceWithSymbolsOnMouth;

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;

        private readonly ITimelineEnvironment _timeline;

        private readonly CancellationTokenSource _cancellationSource;

        private readonly TaskQueue _queue;

        private readonly Dictionary<string, IBoard> _boards;
        private readonly Dictionary<string, ICard> _cards;
        private readonly Dictionary<TaskState, IList> _lists;

        private IMe _user;
        private ITrelloFactory _factory;

        #endregion Fields

        #region Properties

        protected IMe User
        {
            get
            {
                return _user ??= _factory.Me(ct: _cancellationSource.Token).Result;
            }
        }

        public string Mention => User?.Mention ?? null;

        public ITrelloOptions Options { get; }

        #endregion Properties

        #region Events

        public event Action<object, ITaskCommon, IEnumerable<string>> Notify;

        #endregion Events

        #region Constructors

        public TrelloService(ITrelloOptions options, ITimelineEnvironment timeline, ITrelloFactory factory)
            : this(options, timeline)
        {
            _factory = factory;
        }

        public TrelloService(ITrelloOptions options, ITimelineEnvironment timeline)
        {
            _logger = LogManager.GetCurrentClassLogger();

            Options = options;

            _timeline = timeline;

            _boards = new Dictionary<string, IBoard>();
            _cards = new Dictionary<string, ICard>();
            _lists = new Dictionary<TaskState, IList>();

            _cancellationSource = new CancellationTokenSource();
            _queue = new TaskQueue(task => task.Handle(this), timeline);
            _queue.Error += (task, error) => _logger?.Error($"failed task: {task.GetType()}, error: `{error}`");
        }

        public void Dispose()
        {
            // Impl correct resource release.
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

            Enqueue(new ActionTask(SyncBoardCards, Options.Sync.Interval) { LastTime = _timeline.TickCount });
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

        public string Handle(ITaskCommon task)
        {
            IBoard board = BoardGetOrAdd();
            IList list = ListGetOrAdd(board, task.Context.Status);
            ICard card = CardGetOrAdd(board, list, task);

            if (card.List?.Id != list.Id)
                card.List = list;

            if (!string.IsNullOrWhiteSpace(task.Context.Name) && card.Name != task.Context.Name)
                card.Name = task.Context.Name;

            if (!string.IsNullOrWhiteSpace(task.Context.Description) && card.Description != task.Context.Description)
                card.Description = task.Context.Description;

            return card.Id;
        }

        private IBoard BoardGetOrAdd()
        {
            // TODO Как мы будем разводтиь понятие ProjectIssue/Board/ProjectMR?
            if (string.IsNullOrWhiteSpace(Options.BoardId) ||
                !_boards.TryGetValue(Options.BoardId, out IBoard board))
            {
                User.Boards.Refresh(ct: _cancellationSource.Token).Wait();

                board =
                    User.Boards.FirstOrDefault(f => f.Id == Options.BoardId) ??
                    User.Boards.Add(Options.BoardName, ct: _cancellationSource.Token).Result;

                Options.BoardId = board.Id;

                // TODO: Добавить обработку/запись исключений для каждой задачи.
                Task.Factory
                    .ContinueWhenAll(new[]
                    {
                        board.Lists.Refresh(ct: _cancellationSource.Token),
                        board.Cards.Refresh(ct: _cancellationSource.Token),
                    }, s => { })
                    .Wait();

                foreach (IList item in board.Lists)
                    item.IsArchived = true;

                var states = Enum.GetValues(typeof(TaskState)).Cast<TaskState>();
                foreach (var item in states.Reverse())
                {
                    var list = board.Lists.Add(item.ToString(), ct: _cancellationSource.Token).Result;
                    _lists[item] = list;
                }
            }

            _boards[board.Id] = board;
            return _boards[board.Id];
        }

        private IList ListGetOrAdd(IBoard board, TaskState status)
        {
            if (!_lists.TryGetValue(status, out IList list))
            {
                list =
                    board.Lists.FirstOrDefault(f => f.Name == status.ToString()) ??
                    board.Lists.Add(status.ToString(), ct: _cancellationSource.Token).Result;
            }

            _lists[status] = list;
            return _lists[status];
        }

        private ICard CardGetOrAdd(IBoard board, IList list, ITaskCommon task)
        {
            if (string.IsNullOrWhiteSpace(task.ExternalId) ||
                !_cards.TryGetValue(task.ExternalId, out ICard card))
            {
                card =
                    board.Cards.FirstOrDefault(f => f.Id == task.ExternalId) ??
                    list.Cards.Add(name: task.Context.Name, description: task.Context.Description, ct: _cancellationSource.Token).Result;

                card.Updated += OnCardUpdated;
            }

            _cards[card.Id] = card;
            return card;
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
            if (!fields.Any(a => a == nameof(ICard.Name) || a == nameof(ICard.Description) || a == nameof(ICard.List)))
                return;

            Notify?.Invoke(this, 
                new TaskCommon 
                { 
                    ExternalId = card.Id, 
                    Context = new TaskContext
                    {
                        Name = card.Name,
                        Description = card.Description,
                        Status = Enum.TryParse<TaskState>(card.List.Name, true, out var state) ? state : TaskState.New
                    },
                },
                new string[] 
                {
                    nameof(TaskContext.Name),
                    nameof(TaskContext.Description),
                    nameof(TaskContext.Status),
                });
        }

        public void WaitSync() => _queue.IsEmpty();

        #endregion Methods
    }
}
