namespace Tasker
{
    public interface ITaskerStrategy
    {
        void Start(ITaskerService service);

        void Stop();
    }
}