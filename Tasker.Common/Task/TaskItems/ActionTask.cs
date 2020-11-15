namespace Tasker.Common.Task
{
    using System;

    using Tasker.Interfaces.Task;

    public class ActionTask : TaskItem<bool>
    {
        #region Fields

        private readonly Func<bool> _action;

        #endregion Fields

        #region Constructors

        public ActionTask(Func<bool> action, long? interval = null, Action<bool> callback = null) : base(callback, interval)
        {
            _action = action;
        }

        #endregion Constructors

        protected override bool HandleImpl(ITaskVisitor visitor)
        {
            return _action?.Invoke() ?? false;
        }
    }
}
