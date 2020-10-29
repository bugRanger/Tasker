namespace Tasker.Tests
{
    using Tasker.Interfaces;

    public partial class TaskControllerTests
    {
        #region Classes

        private class TaskContext : ITaskContext
        {
            public string Status { get; set; }

            public string Name { get; set; }

            public string Desc { get; set; }
        }

        #endregion Methods
    }
}