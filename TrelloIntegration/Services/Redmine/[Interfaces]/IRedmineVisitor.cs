namespace TrelloIntegration.Services.Redmine
{
    using System;

    using TrelloIntegration.Common.Tasks;
    using TrelloIntegration.Services.Redmine.Tasks;

    interface IRedmineVisitor : IServiceVisitor
    {

        bool Handle(UpdateWorkTimeTask task);

        bool Handle(UpdateIssueTask task);
    }
}
