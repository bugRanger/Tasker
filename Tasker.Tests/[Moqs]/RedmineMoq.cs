namespace Tasker.Tests
{
    using System;
    using System.Net;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using Moq;
    
    using RedmineApi.Core.Types;

    using Framework.Tests;

    using Services.Redmine;
    using System.Xml;
    using System.IO;
    using System.Text;

    internal class RedmineMoq
    {
        #region Classes

        internal class CreateIssue : MethodCallEntry
        {
            internal CreateIssue(Issue issue) : base(issue) { }
        }

        internal class DeleteIssue : MethodCallEntry
        {
            internal DeleteIssue(string issueId, HttpStatusCode code) : base(issueId, code) { }
        }

        internal class GetIssue : TaskCallEntry
        {
            internal GetIssue(string issueId, Issue issue) : base(issueId, issue.Subject, issue.Description, issue.Status.Name) { }

            internal GetIssue(string id, string name, string desc, string state) : base(id, name, desc, state) { }
        }

        internal class GetStatus : MethodCallEntry
        {
            internal GetStatus(string statusId, IssueStatus status) : base(statusId, status) { }
        }

        internal class UpdateIssue : TaskCallEntry
        {
            internal UpdateIssue(string issueId, Issue issue) : base(issueId, issue.Subject, issue.Description, issue.Status.Name) { }

            internal UpdateIssue(string id, string name, string desc, string state) : base(id, name, desc, state) { }
        }

        #endregion Classes

        #region Constants

        private const int STATUS_ID = 10;
        private const int ISSUE_ID = 20;

        private string[] STATUSES = new string[]
        {
            "<issue_status><id>18</id><name>New</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>54</id><name>Req Review</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>20</id><name>specify</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>51</id><name>In Approvement</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>37</id><name>In Analysis</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>22</id><name>In Progress</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>25</id><name>needs clarification</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>31</id><name>On Review</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>38</id><name>In Verification</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>58</id><name>Resolved</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>52</id><name>Paused</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>39</id><name>Blocked</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>30</id><name>verify</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>24</id><name>ready</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>55</id><name>For Dev Estimation</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>56</id><name>In Dev Estimation</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>57</id><name>PdM review</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>5</id><name>Closed</name><is_closed>true</is_closed></issue_status>",
            "<issue_status><id>35</id><name>research</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>29</id><name>estimation</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>28</id><name>ready for implement</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>21</id><name>planned</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>32</id><name>documenting</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>34</id><name>waiting for...</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>26</id><name>done</name><is_closed>true</is_closed></issue_status>",
            "<issue_status><id>27</id><name>rejected</name><is_closed>true</is_closed></issue_status>",
            "<issue_status><id>23</id><name>testing</name><is_closed>false</is_closed></issue_status>",
            "<issue_status><id>53</id><name>suspended</name><is_closed>true</is_closed></issue_status>",
        };

        private string ISSUE_STATUS =
            "<issue_status><id>{id}</id><name>{name}</name><is_closed>false</is_closed></issue_status>";

        private string ISSUE =
            "<issue>" +
            "<id>{id}</id>" +
            "<project id=\"82\" name=\"Administrative activities\"/>" +
            "<tracker id=\"6\" name=\"Task\"/>" +
            "<status id=\"1\" name=\"New\"/>" +
            "<priority id=\"116\" name=\"Unknown\"/>" +
            "<author id=\"163\" name=\"Anikaev Nikita\"/>" +
            "<assigned_to id=\"163\" name=\"Anikaev Nikita\"/>" +
            "<subject>{subject}</subject>" +
            "<description>{description}</description>" +
            "<start_date/>" +
            "<due_date/>" +
            "<done_ratio>0</done_ratio>" +
            "<is_private>false</is_private>" +
            "<estimated_hours>8.0</estimated_hours>" +
            "<total_estimated_hours>8.0</total_estimated_hours>" +
            "<spent_hours>0.0028324564918875694</spent_hours>" +
            "<total_spent_hours>0.0028324564918875694</total_spent_hours>" +
            "<custom_fields type=\"array\">" +
            "<custom_field id=\"92\" name=\"Build\">" +
            "<value/>" +
            "</custom_field>" +
            "</custom_fields>" +
            "<created_on>2020-04-07T03:34:17Z</created_on>" +
            "<updated_on>2020-07-19T15:22:20Z</updated_on>" +
            "<closed_on/>" +
            "</issue>";

        #endregion Constants

        #region Properties

        public Mock<IRedmineProxy> Proxy { get; }

        public Dictionary<string, Issue> Issues { get; }

        public Dictionary<string, IssueStatus> Statuses { get; }

        #endregion Properties

        #region Constructors

        public RedmineMoq(Action<MethodCallEntry> handleEvent) 
        {
            Issues = new Dictionary<string, Issue>();
            Statuses = new Dictionary<string, IssueStatus>();

            Proxy = new Mock<IRedmineProxy>();
            Proxy.Setup(x => x.Create(It.IsAny<Issue>())).Returns<Issue>(source =>
            {
                Issue issue = MakeIssue(source, true, item => item.Id = GetIssueId(Issues.Count + 1));

                handleEvent?.Invoke(new CreateIssue(issue));

                return Task.FromResult(issue);
            });
            Proxy.Setup(x => x.Delete<Issue>(It.IsAny<string>())).Returns<string>(issueId =>
            {
                var result = Issues.Remove(issueId) ? HttpStatusCode.OK : HttpStatusCode.NotFound;

                handleEvent?.Invoke(new DeleteIssue(issueId, result));

                return Task.FromResult(result);
            });
            Proxy.Setup(x => x.Get<Issue>(It.IsAny<string>(), It.IsAny<NameValueCollection>())).Returns<string, NameValueCollection>((id, valiables) =>
            {
                if (Issues.TryGetValue(id, out var issue))
                    issue = MakeIssue(issue, false);

                handleEvent?.Invoke(new GetIssue(id, issue));

                return Task.FromResult(issue);
            });
            Proxy.Setup(x => x.Get<IssueStatus>(It.IsAny<string>(), It.IsAny<NameValueCollection>())).Returns<string, NameValueCollection>((id, valiables) =>
            {
                if (Statuses.TryGetValue(id, out var status))
                    status = MakeStatus(status.Id, status.Name, false);

                handleEvent?.Invoke(new GetStatus(id, status));

                return Task.FromResult(status);
            });
            Proxy.Setup(x => x.ListAll<Issue>(It.IsAny<NameValueCollection>())).Returns<NameValueCollection>((valiables) =>
            {
                return Task.FromResult(Issues.Values.Select(issue => MakeIssue(issue, false)).ToList());
            });
            Proxy.Setup(x => x.ListAll<IssueStatus>(It.IsAny<NameValueCollection>())).Returns<NameValueCollection>((valiables) =>
            {
                return Task.FromResult(Statuses.Values.Select(s => MakeStatus(s.Id, s.Name, false)).ToList());
            });
            Proxy.Setup(x => x.Update(It.IsAny<string>(), It.IsAny<Issue>())).Returns<string, Issue>((id, issue) => 
            {
                if (!Issues.ContainsKey(id))
                {
                    issue = null;
                }
                else
                {
                    issue = MakeIssue(issue, true);
                }

                handleEvent?.Invoke(new UpdateIssue(id, issue));

                return Task.FromResult(issue);
            });
        }

        internal IssueStatus MakeStatus(int id, string name, bool append)
        {
            var statusXml = ISSUE_STATUS
                .Replace("{id}", id.ToString())
                .Replace("{name}", name);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(statusXml));
            using var reader = XmlReader.Create(stream);
            var status = new IssueStatus();
            status.ReadXml(reader);

            if (append)
                Statuses[status.Id.ToString()] = status;

            return status;
        }

        internal Issue MakeIssue(Issue issue, bool append, Action<Issue> prepare = null) 
        {
            return MakeIssue(issue.Id, issue.Subject, issue.Description, append, item =>
            {
                item.Status = MakeStatus(issue.Status.Id, issue.Status.Name, false);
                prepare?.Invoke(item);
            });
        }

        internal Issue MakeIssue(int id, string subject, string description, bool append, Action<Issue> prepare = null)
        {
            var issueXml = ISSUE
                .Replace("{id}", id.ToString())
                .Replace("{subject}", subject)
                .Replace("{description}", description);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(issueXml));
            using var reader = XmlReader.Create(stream);
            var issue = new Issue();
            issue.ReadXml(reader);

            prepare?.Invoke(issue);
            if (append)
                Issues[issue.Id.ToString()] = MakeIssue(issue, false, prepare);

            return issue;
        }

        #endregion Constructors

        #region Methods

        public static int GetStatusId(int value) => STATUS_ID + value;
        public static int GetIssueId(int value) => ISSUE_ID + value;

        #endregion Methods
    }
}
