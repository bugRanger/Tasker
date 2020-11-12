namespace Services.Redmine
{
    public interface IRedmineSync 
    {
        int Interval { get; }

        int UserId { get; }
    }
}
