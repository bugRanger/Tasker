namespace Redmine2Trello.Services
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using Redmine2Trello.Common;
    using Redmine2Trello.Services.Trello.Tasks;

    using Manatee.Trello;
    
    class TrelloService : TaskService, IDisposable
    {
        #region Fields

        private IMe _me;
        private ITrelloOptions _options;
        private TrelloFactory _factory;
        private Dictionary<string, IBoard> _boards;
        private TaskQueue<TaskItem<TrelloService>> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<IBoard> NewBoard;
        public event EventHandler<ICard> UpdateCard;
        public event EventHandler<IssueCard> ImportCard;

        #endregion Events

        #region Constructors

        public TrelloService(ITrelloOptions options)
        {
            TrelloAuthorization.Default.AppKey = options.AppKey;
            TrelloAuthorization.Default.UserToken = options.Token;

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _factory = new TrelloFactory();
            _boards = new Dictionary<string, IBoard>();
            _queue = new TaskQueue<TaskItem<TrelloService>>(task => task.Handle(this));
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion Constructors

        #region Methods

        public override void Start()
        {
            if (_queue.HasEnabled())
                return;

            _queue.Start();
            Enqueue(new SyncListTask(_options.Sync));
        }

        public override void Stop()
        {
            if (!_queue.HasEnabled())
                return;

            _cancellationSource.Cancel();
            _queue.Stop();
        }

        public void Enqueue(TaskItem<TrelloService> task)
        {
            _queue.Enqueue(task);
        }

        public bool Handle(ImportIssueTask task)
        {
            _me = _me ?? _factory.Me().Result;
            _me.Boards.Refresh(true, ct: _cancellationSource.Token).Wait();

            // TODO Add parameter task boardId is not new.
            IBoard board = _me.Boards.FirstOrDefault(a => a.Name == task.IssueCard.Project);
            if (board == null)
            {
                board = _me.Boards.Add(task.IssueCard.Project, ct: _cancellationSource.Token).Result;
                NewBoard?.Invoke(this, board);
            }

            _options.Sync.BoardIds.Add(board.Id);

            board.Lists.Refresh(true, ct: _cancellationSource.Token).Wait();
            IList list = 
                board.Lists.FirstOrDefault(a => a.Name == task.IssueCard.Status) ?? 
                board.Lists.Add(task.IssueCard.Status, ct: _cancellationSource.Token).Result;

            board.Cards.Refresh(true, ct: _cancellationSource.Token).Wait();
            ICard card = board.Cards.FirstOrDefault(a => a.Name == task.IssueCard.Subject);

            if (card == null)
            {
                card = list.Cards.Add(task.IssueCard.Subject, ct: _cancellationSource.Token).Result;
            }
            else if (card.List.Id != list.Id)
            {
                card.List = list;
            }

            ImportCard?.Invoke(this, 
                new IssueCard()
                {
                    CardId = card.Id,
                    IssueId = task.IssueCard.IssueId,
                    Project = task.IssueCard.Project,
                    Subject = task.IssueCard.Subject,
                    Status = card.List.Name
                });

            return true;
        }

        public bool Handle(UpdateListTask task)
        {
            IBoard board = _factory.Board(task.BoardId);
            board.Lists.Refresh(true, ct: _cancellationSource.Token).Wait();
            foreach (string list in task.Lists)
            {
                if (board.Lists.All(a => a.Name != list))
                    board.Lists.Add(list, ct: _cancellationSource.Token);
            }

            return true;
        }

        public bool Handle(SyncListTask task)
        {
            foreach (string boardId in task.SyncOptions.BoardIds)
            {
                if (!_boards.ContainsKey(boardId))
                    _boards[boardId] = _factory.Board(boardId);

                _boards[boardId].Lists.Refresh(true, ct: _cancellationSource.Token).Wait();
                foreach (IList list in _boards[boardId].Lists)
                {
                    foreach (ICard card in list.Cards)
                    {
                        UpdateCard?.Invoke(this, card);
                    }
                }
            }

            if (_queue.HasEnabled())
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_options.Sync.Interval);
                    Enqueue(new SyncListTask(_options.Sync));
                });

            return true;
        }

        #endregion Methods
    }
}
