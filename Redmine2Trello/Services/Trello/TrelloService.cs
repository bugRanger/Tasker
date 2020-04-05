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

    class NewCardEventArgs
    {
        public int IssueId { get; }

        public ICard Card { get; }

        public NewCardEventArgs(int issueId, ICard card) 
        {
            IssueId = issueId;
            Card = card;
        }
    }

    class TrelloService : TaskService, IDisposable
    {
        #region Fields

        private IMe _me;
        private ITrelloOptions _options;
        private TrelloFactory _factory;
        private Dictionary<string, string> _cards2list;
        private TaskQueue<TaskItem<TrelloService>> _queue;
        private CancellationTokenSource _cancellationSource;

        #endregion Fields

        #region Events

        public event EventHandler<IBoard> NewBoard;
        public event EventHandler<NewCardEventArgs> NewCard;

        public event EventHandler<ICard[]> UpdateCards;

        #endregion Events

        #region Constructors

        public TrelloService(ITrelloOptions options)
        {
            TrelloAuthorization.Default.AppKey = options.AppKey;
            TrelloAuthorization.Default.UserToken = options.Token;

            _cancellationSource = new CancellationTokenSource();
            _options = options;
            _factory = new TrelloFactory();
            _cards2list = new Dictionary<string, string>();
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

        public void Handle(ImportIssueTask task)
        {
            _me = _me ?? _factory.Me().Result;
            _me.Boards.Refresh(true, ct: _cancellationSource.Token).Wait();

            // TODO Add parameter task boardId is not new.
            IBoard board = _me.Boards.FirstOrDefault(a => a.Name == task.Project);
            if (board == null)
            {
                board = _me.Boards.Add(task.Project, ct: _cancellationSource.Token).Result;

                _options.Sync.BoardIds.Add(board.Id);
                NewBoard?.Invoke(this, board);
            }

            board.Cards.Refresh(true, ct: _cancellationSource.Token).Wait();
            ICard card = board.Cards.FirstOrDefault(a => a.Name == task.Subject);
            if (card == null)
            {
                board.Lists.Refresh(true, ct: _cancellationSource.Token).Wait();
                IList list = board.Lists.FirstOrDefault(a => a.Name == task.Status) ?? board.Lists.Add(task.Status, ct: _cancellationSource.Token).Result;

                card = list.Cards.Add(task.Subject, ct: _cancellationSource.Token).Result;
                _cards2list[card.Id] = card.List.Id;
                NewCard?.Invoke(this, new NewCardEventArgs(task.IssueId, card));
            }
        }

        public void Handle(UpdateListTask task)
        {
            IBoard board = _factory.Board(task.BoardId);
            board.Lists.Refresh(true, ct: _cancellationSource.Token).Wait();
            foreach (string list in task.Lists)
            {
                if (board.Lists.All(a => a.Name != list))
                    board.Lists.Add(list, ct: _cancellationSource.Token);
            }
        }

        public void Handle(SyncListTask task)
        {
            foreach (string boardId in task.SyncOptions.BoardIds)
            {
                IBoard board = _factory.Board(boardId);

                board.Lists.Refresh(true, ct: _cancellationSource.Token).Wait();
                foreach (IList list in board.Lists)
                {
                    list.Cards.Refresh(true, ct: _cancellationSource.Token).Wait();

                    ICard[] updates = list.Cards
                        .Where(w =>
                            !_cards2list.ContainsKey(w.Id) ||
                            _cards2list[w.Id] != w.List.Id)
                        .ToArray();

                    foreach (ICard card in updates)
                        _cards2list[card.Id] = card.List.Id;

                    if (updates.Any())
                        UpdateCards?.Invoke(this, updates);
                }
            }

            if (_queue.HasEnabled())
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_options.Sync.Interval);
                    Enqueue(new SyncListTask(_options.Sync));
                });
        }

        #endregion Methods
    }
}
