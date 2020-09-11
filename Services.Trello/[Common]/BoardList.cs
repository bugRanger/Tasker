namespace Services.Trello
{
    public class BoardList
    {
        public string CardId { get; }

        public string PrevListId { get; }

        public string CurrListId { get; }

        public BoardList(string cardId, string prevId, string currId)
        {
            CardId = cardId;
            PrevListId = prevId;
            CurrListId = currId;
        }
    }
}
