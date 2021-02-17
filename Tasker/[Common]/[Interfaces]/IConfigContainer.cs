namespace Tasker
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConfigContainer
    {
        #region Methods

        Task<IConfigContainer> Load(IConfigProvider provider, CancellationToken token = default);

        Task<IConfigContainer> Save(IConfigProvider provider, CancellationToken token = default);

        #endregion Methods
    }
}