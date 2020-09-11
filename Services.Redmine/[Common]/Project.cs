namespace Services.Redmine
{
    using System;

    using RedmineApi.Core.Types;

    public class Project
    {
        public int Id { get; internal set; }

        public string Name { get; internal set; }
        
        public Project(IdentifiableName project)
        {
            Id = project.Id;
            Name = project.Name;
        }
    }
}
