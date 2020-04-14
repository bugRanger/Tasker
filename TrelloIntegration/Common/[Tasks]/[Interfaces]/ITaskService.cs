namespace TrelloIntegration.Common
{
    using System;

    interface ITaskService
    {
        event EventHandler<string> Error;

        void Start();

        void Stop();
    }
}
