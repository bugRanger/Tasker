namespace TrelloIntegration.Services
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using RedmineApi.Core;
    using System.Security.Cryptography.X509Certificates;
    using System.Net.Security;
    using System.Security.Authentication;

    internal sealed class DefaultRedmineHttpSettings : IRedmineHttpSettings
    {
        private DefaultRedmineHttpSettings()
        {
            DecompressionMethods = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;
        }

        public IWebProxy WebProxy { get; private set; }
        public AuthenticationHeaderValue Authentication { get; private set; }
        public AuthenticationHeaderValue ProxyAuthentication { get; private set; }
        public CookieContainer CookieContainer { get; private set; }
        public ICredentials DefaultCredentials { get; private set; }
        public ICredentials ProxyCredentials { get; private set; }
        public SslProtocols SslProtocols { get; private set; }
        public DecompressionMethods DecompressionMethods { get; private set; }
        public bool UseProxy { get; private set; }
        public bool UseDefaultCredentials { get; private set; }
        public bool UseCookies { get; private set; }
        public bool PreAuthenticate { get; private set; }

        public bool VerifyServerCertificate { get; private set; } = true;

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get;
            set;
        }

        public bool AllowAutoRedirect { get; private set; }

        public int MaxAutomaticRedirections { get; private set; }

        public IRedmineHttpSettings SetWebProxy(IWebProxy webProxy)
        {
            WebProxy = webProxy;
            return this;
        }

        public IRedmineHttpSettings SetAuthentication(AuthenticationHeaderValue authenticationHeaderValue)
        {
            Authentication = authenticationHeaderValue;
            return this;
        }

        public IRedmineHttpSettings SetProxyAuthentication(AuthenticationHeaderValue proxyAuthenticationHeaderValue)
        {
            ProxyAuthentication = proxyAuthenticationHeaderValue;
            return this;
        }

        public IRedmineHttpSettings SetCookieContainer(CookieContainer cookieContainer)
        {
            CookieContainer = cookieContainer;
            return this;
        }

        public IRedmineHttpSettings SetDefaultCredentials(ICredentials defaultCredentials)
        {
            DefaultCredentials = defaultCredentials;
            return this;
        }

        public IRedmineHttpSettings SetProxyCredentials(ICredentials proxyCredentials)
        {
            ProxyCredentials = proxyCredentials;
            return this;
        }

        public IRedmineHttpSettings SetSslProtocols(SslProtocols sslProtocols)
        {
            SslProtocols = sslProtocols;
            return this;
        }

        public IRedmineHttpSettings SetDecompressionMethods(DecompressionMethods decompressionMethods)
        {
            DecompressionMethods = decompressionMethods;
            return this;
        }

        public IRedmineHttpSettings SetUseProxy(bool useProxy)
        {
            UseProxy = useProxy;
            return this;
        }

        public IRedmineHttpSettings SetUseDefaultCredentials(bool useDefaultCredentials)
        {
            UseDefaultCredentials = useDefaultCredentials;
            return this;
        }

        public IRedmineHttpSettings SetUseCookies(bool useCookies)
        {
            UseCookies = useCookies;
            return this;
        }

        public IRedmineHttpSettings SetPreAuthenticate(bool preAuthenticate)
        {
            PreAuthenticate = preAuthenticate;
            return this;
        }


        public IRedmineHttpSettings Set(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>
                serverCertificateCustomValidationCallback)
        {
            ServerCertificateCustomValidationCallback = serverCertificateCustomValidationCallback;
            return this;
        }

        public IRedmineHttpSettings SetAllowAutoRedirect(bool allowAutoRedirect)
        {
            AllowAutoRedirect = allowAutoRedirect;
            return this;
        }

        public IRedmineHttpSettings SetMaxAutomaticRedirections(int maxAutomaticRedirections)
        {
            MaxAutomaticRedirections = maxAutomaticRedirections;
            return this;
        }

        public HttpClientHandler Build()
        {
            var handler = new HttpClientHandler();

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods;
            }

            if (handler.SupportsRedirectConfiguration)
            {
                handler.AllowAutoRedirect = AllowAutoRedirect;
                handler.MaxAutomaticRedirections = MaxAutomaticRedirections == 0 ? handler.MaxAutomaticRedirections : MaxAutomaticRedirections;
            }

            handler.UseCookies = UseCookies;
            if (UseCookies)
            {
                if (CookieContainer == null)
                {
                    CookieContainer = new CookieContainer();
                }

                handler.CookieContainer = CookieContainer;
            }

            if (UseProxy)
            {
                handler.UseProxy = UseProxy;
                if (handler.SupportsProxy)
                {
                    handler.Proxy = WebProxy;

                    if (UseProxy && WebProxy != null)
                    {
                        handler.Proxy.Credentials = ProxyCredentials;
                    }
                }
            }

            if (UseDefaultCredentials)
            {
                handler.UseDefaultCredentials = UseDefaultCredentials;
                handler.Credentials = DefaultCredentials;
            }

            handler.PreAuthenticate = PreAuthenticate;

            if (SslProtocols == default(SslProtocols))
            {
                SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
            }

            handler.SslProtocols = SslProtocols;

            if (VerifyServerCertificate)
            {
                if (ServerCertificateCustomValidationCallback == null)
                {
                    handler.ServerCertificateCustomValidationCallback += (message, certificate2, chain, sslErrors) =>
                    {
                        return true;
                        //if (sslErrors == SslPolicyErrors.None)
                        //{
                        //    return true;
                        //}

                        //return false;
                    };
                }
                else
                {
                    handler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;
                }
            }

            return handler;
        }

        public static DefaultRedmineHttpSettings Create()
        {
            return new DefaultRedmineHttpSettings();
        }
    }
}
