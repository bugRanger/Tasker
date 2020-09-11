namespace Services.GitLab
{
    using System;

    public class Branch
    {
        #region Properties

        public string Name { get; set; }

        public string Title { get; set; }

        #endregion Properties

        #region Constructors

        public Branch(string name, string title) 
        {
            Name = name;
            Title = title;
        }

        #endregion Constructors
    }
}
