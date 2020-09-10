namespace Services.GitLab
{
    public interface IGitLabSync 
    {
        int Interval { get; }

        int UserId { get; }

        // TODO: Найти пример фильров.
        string SearchBranches { get; }
    }
}
