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

        private ITrelloOptions _options;
        private TrelloFactory _factory;
        private Dictionary<string, string> _cards2list;
        private TaskQueue<TaskItem<TrelloService>> _queue;

        #endregion Fields

        #region Events

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
            _queue = new TaskQueue<TaskItem<TrelloService>>(obj => obj.Handle(this));
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

            Enqueue(new SyncListTask()
            {
                SyncOptions = _options.Sync
            });
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
        
        public async void Handle(SyncListTask task)
        {
            foreach (var boardId in task.SyncOptions.BoardIds)
            {
                var board = _factory.Board(boardId);

                await board.Lists.Refresh(true);
                foreach (var list in board.Lists)
                {
                    await list.Cards.Refresh(true);

                    var updates = list.Cards
                        .Where(w =>
                            !_cards2list.ContainsKey(w.Id) ||
                            _cards2list[w.Id] != w.List.Id)
                        .ToArray();

                    foreach (var card in updates)
                        _cards2list[card.Id] = card.List.Id;

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
