namespace Services.Redmine
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using RedmineApi.Core;
    using System.Net;

    public class RedmineProxy : IRedmineProxy
    {
        #region Fields

        private readonly RedmineManager _manager;

        #endregion Fields

        #region Constructors

        public RedmineProxy(string host, string apiKey, MimeType mimeType = MimeType.Xml, IRedmineHttpSettings httpClientHandler = null) 
        {
            _manager = new RedmineManager(host, apiKey, mimeType, httpClientHandler);
        }

        public void Dispose()
        {
            _manager.Dispose();
        }

        #endregion Constructors

        #region Methods

        public Task<T> Create<T>(T data) where T : class, new()
        {
            return _manager.Create(data);
        }

        public Task<HttpStatusCode> Delete<T>(string id) where T : class, new()
        {
            return _manager.Delete<T>(id);
        }

        public Task<T> Get<T>(string id, NameValueCollection parameters) where T : class, new()
        {
            return _manager.Get<T>(id, parameters);
        }

        public Task<List<T>> ListAll<T>(NameValueCollection parameters) where T : class, new()
        {
            return _manager.ListAll<T>(parameters);
        }

        public Task<T> Update<T>(string id, T data) where T : class, new()
        {
            return _manager.Update<T>(id, data);
        }

        #endregion Methods
    }
}
