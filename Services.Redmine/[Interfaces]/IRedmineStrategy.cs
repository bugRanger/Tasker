namespace Services.Redmine
{
    using System;
    
    public interface IRedmineStrategy
    {
        #region Methods
        
        void Register(IRedmineService visitor);

        void UpdateProjects(Project[] projects);

        void UpdateIssues(Issue[] issues);

        void UpdateStatuses(IssueStatus[] statuses);

        #endregion Methods
    }
}
