namespace Tasker.Tests
{
    using System;

    using Framework.Tests;

    abstract class TaskCallEntry : MethodCallEntry
    {
        protected TaskCallEntry(string id, string name, string desc, string status) : base(id, name, desc, status) { }
    }
}
