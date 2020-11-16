namespace Services.Gitlab
{
    public interface IGitLabOptions
    {
        #region Properties

        int ProjectId { get; }

        int AssignedId { get; }

        string Host { get; }

        string Token { get; }

        string TargetBranch { get; }

        IGitlabSync Sync { get; }

        #endregion Properties
    }
}
