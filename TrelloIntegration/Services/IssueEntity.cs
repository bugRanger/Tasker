namespace TrelloIntegration.Services
{
    using System;

    class IssueEntity
    {
        /// <summary>
        /// Redmine issue id.
        /// </summary>
        public int IssueId { get; set; }

        /// <summary>
        /// Trello card id.
        /// </summary>
        public string CardId { get; set; }

        public string Project { get; set; }

        public string Discription { get; set; }

        public string Subject { get; set; }
        
        public string Status { get; set; }

        public DateTime? UpdateDT { get; set; }
    }    
}
