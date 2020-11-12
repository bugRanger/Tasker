namespace Tasker.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using Moq;

    using Manatee.Trello;

    using Framework.Tests;

    internal class TrelloMoq
    {
        #region Classes

        internal class AppendBoard : MethodCallEntry
        {
            internal AppendBoard(IBoard board) : this(board.Id, board.Name) { }

            internal AppendBoard(string id, string name) : base(id, name) { }
        }

        internal class AppendCard : TaskCallEntry
        {
            internal AppendCard(ICard card) : this(card.Id, card.Name, card.Description, card.List.Name) { }

            internal AppendCard(string id, string name, string desc, string state) : base(id, name, desc, state) { }
        }

        internal class AppendList : MethodCallEntry
        {
            internal AppendList(IList list) : base(list.Id, list.Name) { }

            internal AppendList(string id, string name) : base(id, name) { }
        }

        internal class AppendField : MethodCallEntry
        {
            internal AppendField(ICustomFieldDefinition field) : this(field.Id, field.Name, field.Type, field.Options) { }

            internal AppendField(string id, string name, CustomFieldType? type, IDropDownOptionCollection options) : base(id, name, type, options) { }
        }

        #endregion Classes

        #region Constants

        private const int BOARD_ID = 100;
        private const int LIST_ID = 200;
        private const int CARD_ID = 300;
        private const int FIELD_ID = 400;

        #endregion Constants

        #region Fields

        private readonly List<Mock<IBoard>> _boardContainer;
        private readonly List<Mock<IList>> _listContainer;
        private readonly List<Mock<ICard>> _cardContainer;
        private readonly List<Mock<ICustomFieldDefinition>> _fieldContainer;

        #endregion Fields

        #region Properties

        public Mock<IMe> User { get; }

        public Mock<IBoardCollection> Boards { get; }

        public Mock<ICardCollection> Cards { get; }

        public Mock<IListCollection> Lists { get; }

        public Mock<ITrelloFactory> Factory { get; }

        public Mock<ICustomFieldDefinitionCollection> Fields { get; }

        #endregion Properties

        #region Constuctors

        public TrelloMoq(Action<MethodCallEntry> handleEvent)
        {
            User = new Mock<IMe>();
            Boards = new Mock<IBoardCollection>();
            Lists = new Mock<IListCollection>();
            Cards = new Mock<ICardCollection>();
            Fields = new Mock<ICustomFieldDefinitionCollection>();
            Factory = new Mock<ITrelloFactory>();

            _boardContainer = new List<Mock<IBoard>>();
            _listContainer = new List<Mock<IList>>();
            _cardContainer = new List<Mock<ICard>>();
            _fieldContainer = new List<Mock<ICustomFieldDefinition>>();

            Factory
                .Setup(x => x.Me(It.IsAny<TrelloAuthorization>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(User.Object));

            User.Setup(x => x.Boards).Returns(Boards.Object);
            User.Setup(x => x.Cards).Returns(Cards.Object);

            Boards
                .Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IBoard>(), It.IsAny<CancellationToken>()))
                .Returns<string, string, IBoard, CancellationToken>((name, desc, board, ct) =>
                {
                    var boardMoq = new Mock<IBoard>();
                    var boardId = GetBoardId(_boardContainer.Count + 1);

                    boardMoq.Setup(x => x.Id).Returns(boardId);
                    boardMoq.Setup(x => x.Name).Returns(name);
                    boardMoq.Setup(x => x.Description).Returns(desc);
                    boardMoq.Setup(x => x.Cards).Returns(Cards.Object);
                    boardMoq.Setup(x => x.Lists).Returns(Lists.Object);
                    boardMoq.Setup(x => x.CustomFields).Returns(Fields.Object);

                    Cards.Setup(x => x.Add(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Position>(),
                        It.IsAny<DateTime?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<IEnumerable<IMember>>(),
                        It.IsAny<IEnumerable<ILabel>>(),
                        It.IsAny<CancellationToken>()))
                        .Returns<string, string, Position, DateTime?, bool?, IEnumerable<IMember>, IEnumerable<ILabel>, CancellationToken>((name, desc, position, dt, complite, members, labels, ct) =>
                        {
                            var cardMoq = new Mock<ICard>();
                            var cardId = GetCardId(_cardContainer.Count + 1);

                            IList list = null;

                            cardMoq.Setup(x => x.Board).Returns(boardMoq.Object);
                            cardMoq.Setup(x => x.Id).Returns(cardId);
                            cardMoq.Setup(x => x.Name).Returns(name);
                            cardMoq.Setup(x => x.Description).Returns(desc);
                            cardMoq.Setup(x => x.Position).Returns(position);
                            cardMoq.SetupGet(x => x.List).Returns(list);
                            cardMoq.SetupSet(x => x.List = It.IsAny<IList>()).Callback<IList>(lst => cardMoq.SetupGet(x => x.List).Returns(lst));

                            _cardContainer.Add(cardMoq);

                            handleEvent?.Invoke(new AppendCard(cardMoq.Object));

                            return Task.FromResult(cardMoq.Object);
                        });
                    Cards.Setup(x => x.GetEnumerator())
                        .Returns(() => _cardContainer.Select(s => s.Object).GetEnumerator());

                    Lists.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<Position>(), It.IsAny<CancellationToken>()))
                        .Returns<string, Position, CancellationToken>((name, position, ct) =>
                        {
                            var listMoq = new Mock<IList>();
                            var listId = GetListId(_listContainer.Count + 1);

                            var cardList = new List<Mock<ICard>>();
                            var cards = new Mock<ICardCollection>();

                            listMoq.Setup(x => x.Id).Returns(listId);
                            listMoq.Setup(x => x.Name).Returns(name);
                            listMoq.Setup(x => x.Position).Returns(position);
                            listMoq.Setup(x => x.Board).Returns(boardMoq.Object);
                            listMoq.Setup(x => x.Cards).Returns(cards.Object);

                            cards.Setup(x => x.Add(
                                It.IsAny<string>(),
                                It.IsAny<string>(),
                                It.IsAny<Position>(),
                                It.IsAny<DateTime?>(),
                                It.IsAny<bool?>(),
                                It.IsAny<IEnumerable<IMember>>(),
                                It.IsAny<IEnumerable<ILabel>>(),
                                It.IsAny<CancellationToken>()))
                                .Returns<string, string, Position, DateTime?, bool?, IEnumerable<IMember>, IEnumerable<ILabel>, CancellationToken>((name, desc, position, dt, complite, members, labels, ct) =>
                                {
                                    var cardMoq = new Mock<ICard>();
                                    var cardId = GetCardId(_cardContainer.Count + 1);

                                    cardMoq.Setup(x => x.Board).Returns(boardMoq.Object);
                                    cardMoq.Setup(x => x.Id).Returns(cardId);
                                    cardMoq.Setup(x => x.Name).Returns(name);
                                    cardMoq.Setup(x => x.Description).Returns(desc);
                                    cardMoq.Setup(x => x.Position).Returns(position);
                                    cardMoq.SetupGet(x => x.List).Returns(listMoq.Object);
                                    cardMoq.SetupSet(x => x.List = It.IsAny<IList>()).Callback<IList>(lst => cardMoq.SetupGet(x => x.List).Returns(lst));

                                    cardList.Add(cardMoq);
                                    _cardContainer.Add(cardMoq);

                                    handleEvent?.Invoke(new AppendCard(cardMoq.Object));

                                    return Task.FromResult(cardMoq.Object);
                                });
                            cards.Setup(x => x.GetEnumerator())
                                .Returns(() => cardList.Select(s => s.Object).GetEnumerator());

                            _listContainer.Add(listMoq);

                            handleEvent?.Invoke(new AppendList(listMoq.Object));

                            return Task.FromResult(listMoq.Object);
                        });
                    Lists.Setup(x => x.GetEnumerator())
                        .Returns(() => _listContainer.Select(s => s.Object).GetEnumerator());

                    Fields.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<CustomFieldType>(), It.IsAny<CancellationToken>(), It.IsAny<IDropDownOption[]>()))
                        .Returns<string, CustomFieldType, CancellationToken, IDropDownOption[]>((name, type, ct, options) => 
                        {
                            var fieldMoq = new Mock<ICustomFieldDefinition>();
                            var fieldId = GetFieldId(_fieldContainer.Count + 1);

                            var optionMoq = new Mock<IDropDownOptionCollection>();
                            optionMoq.Setup(x => x.GetEnumerator()).Returns(options.Cast<IDropDownOption>().GetEnumerator());

                            fieldMoq.Setup(x => x.Id).Returns(fieldId);
                            fieldMoq.Setup(x => x.Board).Returns(boardMoq.Object);
                            fieldMoq.Setup(x => x.Name).Returns(name);
                            fieldMoq.Setup(x => x.Type).Returns(type);
                            fieldMoq.Setup(x => x.Options).Returns(optionMoq.Object);

                            _fieldContainer.Add(fieldMoq);

                            handleEvent?.Invoke(new AppendField(fieldMoq.Object));

                            return Task.FromResult(fieldMoq.Object);
                        });
                    Fields.Setup(x => x.GetEnumerator())
                        .Returns(() => _fieldContainer.Select(s => s.Object).GetEnumerator());

                    _boardContainer.Add(boardMoq);

                    handleEvent?.Invoke(new AppendBoard(boardMoq.Object));

                    return Task.FromResult(boardMoq.Object);
                });

            Boards.Setup(x => x.GetEnumerator()).Returns(() => _boardContainer.Select(board => board.Object).GetEnumerator());

            // TODO Check param auth.
            var factory = new Mock<ITrelloFactory>();
            factory
                .Setup(x => x.Me(It.IsAny<TrelloAuthorization>(), It.IsAny<CancellationToken>()))
                .Returns<TrelloAuthorization, CancellationToken>((auth, ct) => Task.FromResult(User.Object));
        }

        #endregion Constructors

        // TODO Add check methods.
        #region Methods

        public void RaiseNotify(string cardId, string statusName)
        {
            var card = _cardContainer.FirstOrDefault(f => f.Object.Id == cardId);
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            var status = _listContainer.FirstOrDefault(f => f.Object.Name == statusName);
            if (status == null)
                throw new ArgumentNullException(nameof(status));

            card.SetupGet(x => x.List).Returns(status.Object);
            card.Raise(x => x.Updated += null, card.Object, 
                new string[] 
                { 
                    nameof(ICard.Name),
                    nameof(ICard.Description),
                    nameof(ICard.List),
                });
        }

        public static string GetBoardId(int value) => (BOARD_ID + value).ToString();
        public static string GetListId(int value) => (LIST_ID + value).ToString();
        public static string GetCardId(int value) => (CARD_ID + value).ToString();
        public static string GetFieldId(int value) => (FIELD_ID + value).ToString();

        #endregion Methods
    }
}
