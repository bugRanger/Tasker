namespace Services.Gitlab
{
    public interface IGitlabSync 
    {
        int Interval { get; }

        int UserId { get; }
    }
}
