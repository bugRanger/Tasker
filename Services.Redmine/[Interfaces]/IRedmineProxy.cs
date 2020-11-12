namespace Services.Redmine
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    public interface IRedmineProxy : IDisposable
    {
        Task<TData> Create<TData>(TData data) where TData : class, new();

        Task<HttpStatusCode> Delete<T>(string id) where T : class, new();

        Task<List<TData>> ListAll<TData>(NameValueCollection parameters) where TData : class, new();

        Task<TData> Get<TData>(string id, NameValueCollection parameters) where TData : class, new();

        Task<TData> Update<TData>(string id, TData data) where TData : class, new();
    }
}