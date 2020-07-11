namespace Common
{
    using System;

    public interface ITimelineEnviroment
    {
        #region Methods

        /// <summary>
        /// Append new timer action.
        /// </summary>
        /// <param name="interval">Interval execute action.</param>
        /// <param name="action">Action</param>
        /// <returns>If success return timerId.</returns>
        long AppendTimer(int interval, Action action);

        /// <summary>
        /// Remove timer action.
        /// </summary>
        /// <param name="timerId">Timer id.</param>
        void RemoveTimer(long timerId);

        long TickCount();

        #endregion Methods
    }
}
