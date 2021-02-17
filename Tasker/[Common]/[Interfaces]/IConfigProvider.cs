namespace Tasker
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConfigProvider
    {
        #region Methods

        Task<T> Read<T>(IConfigContainer config, CancellationToken token = default) where T : class, new();

        Task Write<T>(IConfigContainer config, T value, CancellationToken token = default) where T : class, new();

        #endregion Methods
    }
}