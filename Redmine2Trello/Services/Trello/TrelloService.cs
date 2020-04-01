namespace Redmine2Trello.Services
{
    using System;
    using System.Threading.Tasks;

    using Redmine2Trello.Common;
    using Redmine2Trello.Services.Trello.Tasks;

    using Manatee.Trello;
    using System.Collections.Generic;
    using System.Linq;

    class TrelloService : TaskService, IDisposable
    {
        #region Fields

        private IMe _me;
        private ITrelloOptions _options;
        private Dictionary<string, string> _cards;
        private TrelloFactory _factory;
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
            _cards = new Dictionary<string, string>();
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

            Enqueue(new ConnectTask());
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

        public async void Handle(ConnectTask task)
        {
            _me = await _factory.Me();

            await _me.Boards.Refresh(true);
            foreach (var board in _me.Boards)
            {
                await board.Lists.Refresh(true);
                foreach (var list in board.Lists)
                {
                    await list.Cards.Refresh(true);
                    Enqueue(new SyncListTask()
                    {
                        ListId = list.Id
                    });
                }
            }
        }

        public async void Handle(SyncListTask task)
        {
            var list = _factory.List(task.ListId);
            await list.Cards.Refresh(true);

            var updates = list.Cards
                .Where(w => 
                    !_cards.ContainsKey(w.Id) || 
                    _cards[w.Id] != w.List.Id)
                .ToArray();

            foreach (var card in updates)
                _cards[card.Id] = card.List.Id;

            UpdateCards?.Invoke(this, updates);

            if (_queue.HasEnabled())
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    Enqueue(new SyncListTask()
                    {
                        ListId = task.ListId
                    });
                });
        }

        //foreach (var board in _me.Boards)
        //    {
        //        await board.Lists.Refresh(true);
        //        foreach (var list in board.Lists)
        //        {
        //            await list.Cards.Refresh(true);
        //        }
        //    }
        #endregion Methods
    }
}
