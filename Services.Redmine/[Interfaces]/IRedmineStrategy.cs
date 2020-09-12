namespace Services.Redmine
{
    using System;
    
    public interface IRedmineBehaviors
    {
        #region Methods

        void UpdateProjects(Project[] projects);

        void UpdateIssues(Issue[] issues);

        void UpdateStatuses(IssueStatus[] statuses);

        #endregion Methods
    }
}
