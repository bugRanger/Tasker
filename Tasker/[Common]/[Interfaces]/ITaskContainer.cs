namespace Tasker
{
    using System.Collections.Generic;

    using Tasker.Common.Task;

    public interface ITaskContainer
    {
        #region Methods

        public bool TryGet(int subjectId, string taskId, out TaskCommon taskCommon);

        public void Set(IReadOnlyDictionary<int, string> subjectToTask, TaskContext context);

        #endregion Methods
    }
}