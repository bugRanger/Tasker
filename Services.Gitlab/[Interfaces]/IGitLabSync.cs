namespace Services.GitLab
{
    public interface IGitLabSync 
    {
        int Interval { get; }

        int UserId { get; }
    }
}
