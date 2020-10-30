namespace Tasker.Tests
{
    using Framework.Tests;

    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    using Tasker.Common.Task;
    using Tasker.Interfaces.Task;

    public partial class TaskControllerTests
    {
        #region Classes

        private class NotifyEntry : MethodCallEntry 
        {
            public NotifyEntry(ITaskCommon task) : base(task) { }
        }

        private class EnqueueEntry : MethodCallEntry
        {
            public EnqueueEntry(ITaskItem task) : base(task) { }
        }

        #endregion Classes

        #region Fields

        private MethodCallList _events;
        private List<Mock<ITaskService>> _services;
        private Dictionary<int, string> _tasks;
        private ITaskController _controller;

        #endregion Fields

        #region Methods

        [SetUp]
        public void Setup()
        {
            _events = new MethodCallList();

            _services = new List<Mock<ITaskService>>();
            _tasks = new Dictionary<int, string>();

            _controller = new TaskController(_tasks);

            for (int i = 0; i < 3; i++)
            {
                var index = i + 1;
                // TODO User real service with mock net-layer.
                var service = new Mock<ITaskService>();

                _services.Add(service);

                // TODO: Use static ID service.
                service
                    .Setup(x => x.GetHashCode())
                    .Returns(index);

                service
                    .Setup(x => x.Enqueue(It.IsAny<ITaskItem>()))
                    .Callback<ITaskItem>(task => 
                    {
                        _events.Add(new EnqueueEntry(task));
                        if (task is UpdateTask updateTask)
                            updateTask.Callback?.Invoke(Convert.ToString(index));
                    });

                _controller.Register(service.Object);
            }
        }

        [Test]
        public void NotifyTest()
        {
            // Arrage

            // Act
            _services[0].Raise(x => x.Notify += null, _services[0].Object, new TaskCommon { Id = Convert.ToString(1), Context = new TaskContext() });
            _services[1].Raise(x => x.Notify += null, _services[1].Object, new TaskCommon { Id = Convert.ToString(2), Context = new TaskContext() });
            _services[2].Raise(x => x.Notify += null, _services[2].Object, new TaskCommon { Id = Convert.ToString(3), Context = new TaskContext() });

            // Assert
            _events.Assert();
        }

        #endregion Methods
    }
}