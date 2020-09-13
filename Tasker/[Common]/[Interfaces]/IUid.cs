namespace Tasker
{
    using System;

    public interface IUid<out T>
    {
        T this[ServiceType type] { get; }
    }
}
