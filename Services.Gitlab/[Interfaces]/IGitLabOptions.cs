using GitLabApiClient.Internal.Paths;

namespace Services.GitLab
{
    public interface IGitLabOptions
    {
        int ProjectId { get; }

        int AssignedId { get; }

        string Host { get; }

        string Token { get; }

        IGitLabSync Sync { get; }
    }
}
