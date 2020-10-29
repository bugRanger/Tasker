namespace Tasker
{
    using System;
    using System.Collections.Generic;

    using Tasker.Interfaces;

    public interface ITaskController
    {
        #region Properties

        IEnumerable<KeyValuePair<int, string>> Cached { get; }

        #endregion Properties

        #region Methods

        void Register(ITaskService service);

        int Load(IEnumerable<KeyValuePair<int, string>> cached);

        #endregion Methods
    }
}
