namespace Utils
{
    using System.Threading;

    public class Locker
    {
        #region Fields

        private const int ENABLED = 1;

        private const int DISABLE = 0;

        private int _state;

        #endregion Fields

        #region Constructors

        public Locker()
        {
            _state = DISABLE;
        }

        #endregion Constructors

        #region Methods


        public bool IsEnabled()
        {
            return Interlocked.CompareExchange(ref _state, ENABLED, ENABLED) == ENABLED;
        }

        public bool SetEnabled()
        {
            return Interlocked.CompareExchange(ref _state, ENABLED, DISABLE) == DISABLE;
        }

        public bool SetDisabled()
        {
            return Interlocked.CompareExchange(ref _state, DISABLE, ENABLED) == ENABLED;
        }

        #endregion Methods
    }
}
