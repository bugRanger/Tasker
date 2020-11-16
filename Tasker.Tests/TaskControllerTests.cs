namespace Tasker.Tests
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;

    using Moq;
    using NUnit.Framework;

    using Framework.Tests;
    using Framework.Timeline;

    using Tasker.Common.Task;
    using Tasker.Interfaces.Task;

    using Services.Trello;
    using Services.Redmine;
    using Services.Gitlab;

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

        #region Constants

        private const int TRELLO_ID = 1;
        private const int REDMINE_ID = 2;
        private const int GITLAB_ID = 3;

        #endregion Constants

        #region Fields

        private MethodCallList _redmineEvents;
        private MethodCallList _trelloEvents;
        private MethodCallList _gitlabEvents;
        private Dictionary<int, TaskCommon> _tasks;
        private ITaskController _controller;

        private TrelloMoq _trelloMoq;
        private TrelloService _trello;

        private RedmineMoq _redmineMoq;
        private RedmineService _redmine;

        private GitlabMoq _gitlabMoq;
        private GitLabService _gitlab;

        #endregion Fields

        #region Methods

        [SetUp]
        public void Setup()
        {
            _redmineEvents = new MethodCallList();
            _trelloEvents = new MethodCallList();
            _gitlabEvents = new MethodCallList();

            _tasks = new Dictionary<int, TaskCommon>();

            _controller = new TaskController(_tasks);

            _redmineMoq = new RedmineMoq(_redmineEvents.Add);
            _trelloMoq = new TrelloMoq(_trelloEvents.Add);
            _gitlabMoq = new GitlabMoq(_gitlabEvents.Add);

            var trelloOptions = new TrelloOptions
            {
                AppKey = "apy-key",
                Token = "api-token",
                Interval = 0,
            };
            var gitlabOptions = new GitLabOptions
            {
                ProjectId = 1,
                Interval = 0,
                TargetBranch = "origin/master",
                AssignedId = 18,
                UserId = 81,
            };
            var redmineOptions = new RedmineOptions()
            {
                Interval = 0,
            };

            _controller.Register(_trello = new TrelloService(trelloOptions, TimelineEnvironment.Instance, _trelloMoq.Factory.Object));
            _controller.Register(_redmine = new RedmineService(redmineOptions, TimelineEnvironment.Instance, _redmineMoq.Proxy.Object));
            _controller.Register(_gitlab = new GitLabService(gitlabOptions, TimelineEnvironment.Instance, _gitlabMoq.Proxy.Object));
        }

        [Test]
        public void RedmineNewTest()
        {
            // Arrage
            var issueId = RedmineMoq.GetIssueId(1).ToString();
            var cardId = TrelloMoq.GetCardId(1);
            var branchId = $"feature/{issueId}";

            var subject = "Name";
            var description = "Description";
            var status = TaskState.New;

            _redmineMoq.MakeIssue(
                RedmineMoq.GetIssueId(1), 
                subject, 
                description, 
                true, 
                s => s.Status = _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(1), status.ToString(), true));

            var context = new TaskContext
            {
                Id = issueId,
                Name = subject,
                Description = description,
                Kind = TaskKind.Task,
                Status = status,
            };

            var expected = new Dictionary<int, TaskCommon>
            {
                {
                    TaskController.GetKey(TRELLO_ID, issueId),
                    new TaskCommon { ExternalId = cardId, Context = context }
                },
                {
                    TaskController.GetKey(TRELLO_ID, branchId),
                    new TaskCommon { ExternalId = cardId, Context = context }
                },

                {
                    TaskController.GetKey(REDMINE_ID, cardId),
                    new TaskCommon { ExternalId = issueId, Context = context }
                },
                {
                    TaskController.GetKey(REDMINE_ID, branchId),
                    new TaskCommon { ExternalId = issueId, Context = context }
                },

                {
                    TaskController.GetKey(GITLAB_ID, cardId),
                    new TaskCommon { ExternalId = branchId, Context = context }
                },
                {
                    TaskController.GetKey(GITLAB_ID, issueId),
                    new TaskCommon { ExternalId = branchId, Context = context }
                },
            };

            // Act
            _controller.Start();

            _redmine.WaitSync();
            _trello.WaitSync();
            _gitlab.WaitSync();

            // Assert
            CollectionAssert.AreEqual(expected, _tasks);

            _gitlabEvents.Assert();
            _trelloEvents.Assert(
                new TrelloMoq.AppendBoard(_trello.Options.BoardId, _trello.Options.BoardName),
                new TrelloMoq.AppendList(TrelloMoq.GetListId(1), "Closed"),
                new TrelloMoq.AppendList(TrelloMoq.GetListId(2), "Paused"),
                new TrelloMoq.AppendList(TrelloMoq.GetListId(3), "Resolved"),
                new TrelloMoq.AppendList(TrelloMoq.GetListId(4), "OnReview"),
                new TrelloMoq.AppendList(TrelloMoq.GetListId(5), "InProgress"),
                new TrelloMoq.AppendList(TrelloMoq.GetListId(6), "InAnalysis"),
                new TrelloMoq.AppendList(TrelloMoq.GetListId(7), "New"),
                new TrelloMoq.AppendCard(TrelloMoq.GetCardId(1), subject, description, status.ToString()));
        }

        [Test]
        public void TrelloInAnalysisTest()
        {
            // Arrage
            _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(1), "New", true);
            _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(2), "InAnalysis", true);
            _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(3), "InProgress", true);
            _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(4), "OnReview", true);
            _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(5), "Resolved", true);
            _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(6), "Paused", true);
            _redmineMoq.MakeStatus(RedmineMoq.GetStatusId(7), "Closed", true);

            RedmineNewTest();

            var issueId = RedmineMoq.GetIssueId(1).ToString();
            var cardId = TrelloMoq.GetCardId(1);

            var card = _trelloMoq.Cards.Object.FirstOrDefault(f => f.Id == cardId);
            var oldStatus = card.List.Name;
            var newStatus = TaskState.InAnalysis;

            var context = new TaskContext
            {
                Id = issueId,
                Name = card.Name,
                Description = card.Description,
                Kind = TaskKind.Task,
                Status = newStatus,
            };

            var expected = new Dictionary<int, TaskCommon>(_tasks);
            foreach (var item in expected.ToArray())
            {
                expected[item.Key] = new TaskCommon { ExternalId = item.Value.ExternalId, Context = context };
            }

            // Act
            _trelloMoq.RaiseNotify(card.Id, newStatus.ToString());

            _redmine.WaitSync();
            _trello.WaitSync();
            _gitlab.WaitSync();

            // Assert
            Assert.AreEqual(newStatus.ToString(), card.List.Name);
            CollectionAssert.AreEqual(expected, _tasks);

            _gitlabEvents.Assert();
            _redmineEvents.Assert(
                new RedmineMoq.GetIssue(issueId, card.Name, card.Description, oldStatus),
                new RedmineMoq.UpdateIssue(issueId, card.Name, card.Description, newStatus.ToString()));
        }

        [Test]
        public void GitlabInProgressTest()
        {
            // Arrage
            TrelloInAnalysisTest();

            var issueId = RedmineMoq.GetIssueId(1).ToString();
            var cardId = TrelloMoq.GetCardId(1);
            var branchId = $"feature/{issueId}";

            var card = _trelloMoq.Cards.Object.FirstOrDefault(f => f.Id == cardId);
            var oldStatus = card.List.Name;
            var newStatus = TaskState.InProgress;

            var context = new TaskContext
            {
                Id = issueId,
                Name = card.Name,
                Description = card.Description,
                Kind = TaskKind.Task,
                Status = newStatus,
            };

            var expected = new Dictionary<int, TaskCommon>(_tasks);
            foreach (var item in expected.ToArray())
            {
                expected[item.Key] = new TaskCommon { ExternalId = item.Value.ExternalId, Context = context };
            }

            // Act
            _trelloMoq.RaiseNotify(card.Id, newStatus.ToString());

            _redmine.WaitSync();
            _trello.WaitSync();
            _gitlab.WaitSync();

            // Assert
            Assert.AreEqual(newStatus.ToString(), card.List.Name);
            CollectionAssert.AreEqual(expected, _tasks);

            _redmineEvents.Assert(
                new RedmineMoq.GetIssue(issueId, card.Name, card.Description, oldStatus),
                new RedmineMoq.UpdateIssue(issueId, card.Name, card.Description, newStatus.ToString()));
            _gitlabEvents.Assert();
        }

        [Test]
        public void GitlabOnReviewTest()
        {
            // Arrage
            GitlabInProgressTest();

            var issueId = RedmineMoq.GetIssueId(1).ToString();
            var cardId = TrelloMoq.GetCardId(1);
            var branchId = $"feature/{issueId}";

            var card = _trelloMoq.Cards.Object.FirstOrDefault(f => f.Id == cardId);
            var oldStatus = card.List.Name;
            var newStatus = TaskState.OnReview;

            _gitlabMoq.MakeBranches(branchId, "title");

            var context = new TaskContext
            {
                Id = issueId,
                Name = card.Name,
                Description = card.Description,
                Kind = TaskKind.Task,
                Status = newStatus,
            };

            var expected = new Dictionary<int, TaskCommon>(_tasks);
            foreach (var item in expected.ToArray())
            {
                expected[item.Key] = new TaskCommon { ExternalId = item.Value.ExternalId, Context = context };
            }

            // Act
            _trelloMoq.RaiseNotify(card.Id, newStatus.ToString());

            _redmine.WaitSync();
            _trello.WaitSync();
            _gitlab.WaitSync();

            // Assert
            Assert.AreEqual(newStatus.ToString(), card.List.Name);
            CollectionAssert.AreEqual(expected, _tasks);

            _redmineEvents.Assert(
                new RedmineMoq.GetIssue(issueId, card.Name, card.Description, oldStatus),
                new RedmineMoq.UpdateIssue(issueId, card.Name, card.Description, newStatus.ToString()));
            _gitlabEvents.Assert(
                new GitlabMoq.CreateMergeRequest(1, _gitlab.Options.ProjectId.ToString(), "title", branchId, _gitlab.Options.TargetBranch, _gitlab.Options.AssignedId, true));
        }


        [Test]
        public void RedminePausedTest()
        {
            // Arrage
            TrelloInAnalysisTest();

            var issueId = RedmineMoq.GetIssueId(1).ToString();
            var cardId = TrelloMoq.GetCardId(1);
            var branchId = $"feature/{issueId}";

            var card = _trelloMoq.Cards.Object.FirstOrDefault(f => f.Id == cardId);
            var oldStatus = card.List.Name;
            var newStatus = TaskState.Paused;

            var context = new TaskContext
            {
                Id = issueId,
                Name = card.Name,
                Description = card.Description,
                Kind = TaskKind.Task,
                Status = newStatus,
            };

            var expected = new Dictionary<int, TaskCommon>(_tasks);
            foreach (var item in expected.ToArray())
            {
                expected[item.Key] = new TaskCommon { ExternalId = item.Value.ExternalId, Context = context };
            }

            // Act
            _redmineMoq.Issues[issueId].Status = _redmineMoq.Statuses.Values.FirstOrDefault(f => f.Name == newStatus.ToString());

            _redmine.WaitSync();
            _trello.WaitSync();
            _gitlab.WaitSync();

            // Assert
            Assert.AreEqual(newStatus.ToString(), card.List.Name);
            CollectionAssert.AreEqual(expected, _tasks);

            _redmineEvents.Assert();
            _trelloEvents.Assert();
            _gitlabEvents.Assert();
        }

        [Test]
        public void GetKeyTest()
        {
            // Arrage
            int expected1 = 1286132837;
            int expected2 = -1929052188;

            // Act
            var hash1 = TaskController.GetKey(int.MaxValue, "yGv9F3qmOKf8C7hcpVq6");
            var hash2 = TaskController.GetKey(int.MaxValue, "yGv9F3qmOKf8C7hcpVq6");
            var hash3 = TaskController.GetKey(int.MaxValue - 1, "yGv9F3qmOKf8C7hcpVq6");

            // Assert
            Assert.AreEqual(expected1, hash1);
            Assert.AreEqual(expected1, hash2);
            Assert.AreEqual(expected2, hash3);
        }

        #endregion Methods
    }
}