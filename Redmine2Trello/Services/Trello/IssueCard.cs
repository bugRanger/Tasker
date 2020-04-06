namespace Redmine2Trello.Services
{
    class IssueCard
    {
        public int IssueId { get; set; }

        public string CardId { get; set; }

        public string Project { get; set; }

        public string Subject { get; set; }
        
        public string Status { get; set; }
    }
    
}
