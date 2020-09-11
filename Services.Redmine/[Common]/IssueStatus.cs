namespace Services.Redmine
{
    using System;

    using RedmineApi.Core.Types;

    public class IssueStatus
    {
        public int Id { get; internal set; }

        public string Name { get; internal set; }

        public IssueStatus(IdentifiableName status) 
        {
            Id = status.Id;
            Name = status.Name;
        }
    }
}
