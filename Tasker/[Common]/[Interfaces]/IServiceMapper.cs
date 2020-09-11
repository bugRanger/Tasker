namespace Tasker
{
    using System;

    using Framework.Common;
    using Services.Trello;

    public interface IServiceMapper
    {
        #region Properties

        Mapper<string, int> Card2IssueMapper { get; }

        Mapper<string, int> List2StatusMapper { get; }

        Mapper<string, int> Label2ProjectMapper { get; }

        Mapper<string, int> Branch2IssueMapper { get; }

        Mapper<string, CustomField> Field2FieldMapper { get; }

        #endregion Properties
    }
}
