namespace Services.GitLab
{
    public interface IGitLabOptions
    {
        string Host { get; }

        string Token { get; }

        IGitLabSync Sync { get; }
    }
}
