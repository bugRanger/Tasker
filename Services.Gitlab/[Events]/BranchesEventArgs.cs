namespace Services.GitLab
{
    using System;

    public class BranchesEventArgs
    {
        #region Properties

        public string Name { get; set; }

        public string Title { get; set; }

        #endregion Properties

        #region Constructors

        public BranchesEventArgs(string name, string title) 
        {
            Name = name;
            Title = title;
        }

        #endregion Constructors
    }
}
