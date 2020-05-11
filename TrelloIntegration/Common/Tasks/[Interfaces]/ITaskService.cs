namespace TrelloIntegration.Common.Tasks
{
    using System;

    interface ITaskService 
    {
        event EventHandler<string> Error;

        void Start();

        void Stop();
    }
}
