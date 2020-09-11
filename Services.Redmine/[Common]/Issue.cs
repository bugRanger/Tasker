namespace Services.Redmine
{
    using System;

    using IssueRM = RedmineApi.Core.Types.Issue;

    public class Issue
    {
        public int Id { get; internal set; }

        public Project Project { get; internal set; }

        public string Subject { get; internal set; }

        public string Description { get; internal set; }

        public double? EstimatedHours { get; internal set; }

        public double? SpentHours { get; internal set; }

        public IssueStatus Status { get; internal set; }

        public Issue(IssueRM issue) 
        {
            Id = issue.Id;
            Subject = issue.Subject;
            Description = issue.Description;
            EstimatedHours = issue.EstimatedHours;
            SpentHours = issue.SpentHours;

            Project = new Project(issue.Project);
            Status = new IssueStatus(issue.Status);
        }
    }
}
