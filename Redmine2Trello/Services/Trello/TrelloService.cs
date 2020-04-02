namespace Redmine2Trello.Services
{
    using System;
    using System.Linq;
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
        private Dictionary<string, string> _cards2list;
        private TaskQueue<TaskItem<TrelloService>> _queue;

        #endregion Fields

        #region Events

        public event EventHandler<IBoard> NewBoard;

        public event EventHandler<ICard[]> UpdateCards;

        #endregion Events

        #region Constructors

        public TrelloService(ITrelloOptions options)
        {
            TrelloAuthorization.Default.AppKey = options.AppKey;
            TrelloAuthorization.Default.UserToken = options.Token;

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

            _queue.Stop();
        }

        public void Enqueue(TaskItem<TrelloService> task)
        {
            _queue.Enqueue(task);
        }

        public async void Handle(ImportIssueTask task)
        {
            _me ??= await _factory.Me();

            IBoard board = _me.Boards.FirstOrDefault(a => a.Name != task.Project);
            if (board == null)
            {
                board = await _me.Boards.Add(task.Project);
                _options.Sync.BoardIds.Add(board.Id);
                NewBoard?.Invoke(this, board);
            }

            IList list = board.Lists.FirstOrDefault(a => a.Name == task.Status) ?? await board.Lists.Add(task.Status);
            ICard card = list.Cards.FirstOrDefault(a => a.Name == task.Subject) ?? await list.Cards.Add(task.Subject);
        }

        public async void Handle(UpdateListTask task)
        {
            IBoard board = _factory.Board(task.BoardId);
            await board.Lists.Refresh(true);
            foreach (string list in task.Lists)
            {
                if (board.Lists.All(a => a.Name != list))
                {
                    await board.Lists.Add(list);
                }
            }
        }

        public async void Handle(SyncListTask task)
        {
            foreach (string boardId in task.SyncOptions.BoardIds)
            {
                IBoard board = _factory.Board(boardId);

                await board.Lists.Refresh(true);
                foreach (IList list in board.Lists)
                {
                    await list.Cards.Refresh(true);

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
