namespace Services.Gitlab
{
    public interface IGitlabSync 
    {
        int Interval { get; }

        int UserId { get; }

        // TODO: Найти пример фильров.
        string SearchBranches { get; }
    }
}
