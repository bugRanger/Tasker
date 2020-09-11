namespace Services.GitLab
{
    public interface IGitLabOptions
    {
        #region Properties

        int ProjectId { get; }

        int AssignedId { get; }

        string Host { get; }

        string Token { get; }

        IGitLabSync Sync { get; }

        #endregion Properties
    }
}
