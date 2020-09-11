namespace Services.GitLab
{
    public class MergeRequest
    {
        #region Properties

        public int Id { get; internal set; }

        public string Url { get; internal set; }

        public string Title { get; internal set; }

        public MergeStatus State { get; internal set; }

        public bool WorkInProgress { get; internal set; }

        #endregion Properties
    }
}
