using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NutzCode.Libraries.Web
{
    public class WebParameters : IDisposable
    {
        public class HttpClientInfo
        {
            public HttpClient Client;
            public HttpClientHandler Handler;
            public int Count;
        }

        private static Dictionary<string, HttpClientInfo> _httpClients = new Dictionary<string, HttpClientInfo>();
        
        private (HttpClient, HttpClientHandler) Create(HttpClientHandler handler=null)
        {
            lock(_httpClients)
            {
                HttpClientInfo info;
                if (_httpClients.ContainsKey(Key))
                {
                    info = _httpClients[Key];
                    info.Count++;
                    return (info.Client, info.Handler);
                }
                handler = handler ?? new HttpClientHandler() { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip, MaxConnectionsPerServer = 48 };
                handler.UseCookies = false;
                HttpClient cl = new HttpClient(handler);
                info = new HttpClientInfo();
                info.Client = cl;
                info.Handler = handler;
                info.Count = 1;
                _httpClients[Key] = info;
                return (cl, handler);
            }
        }

        public HttpClient HttpClient
        {
            get
            {
                lock(_httpClients)
                {
                    if (_httpClients.ContainsKey(Key))
                        return _httpClients[Key].Client;
                    return null;
                }
            }
        }
        public HttpClientHandler HttpClientHandler
        {
            get
            {
                lock (_httpClients)
                {
                    if (_httpClients.ContainsKey(Key))
                        return _httpClients[Key].Handler;
                    return null;
                }
            }

        }

        private bool _disposed = false;

        public string Key { get; private set; }
        public Uri Url { get; set; }
        public byte[] PostData { get; set; } = null;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string PostEncoding { get; set; } = "application/x-www-form-urlencoded";
        public string UserAgent { get; set; } = null;
        public List<Cookie> Cookies { get; set; } = null;
        public NameValueCollection Headers { get; set; } = null;
        public int TimeoutInMilliseconds
        {
            get => (int)HttpClient.Timeout.TotalMilliseconds;
            set => HttpClient.Timeout = TimeSpan.FromMilliseconds(value);
        }
        public Uri Referer { get; set; } = null;
        public IWebProxy Proxy
        {
            get => HttpClientHandler.Proxy;
            set => HttpClientHandler.Proxy=value;
        }
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public Func<WebStream, object, CancellationToken, Task> RequestCallback { get; set; } = null;
        public Func<WebStream, object, CancellationToken, Task<bool>> ErrorCallback { get; set; } = null; 
        public bool AutoRedirect
        {
            get => HttpClientHandler.AllowAutoRedirect;
            set =>HttpClientHandler.AllowAutoRedirect=value;
        }
        public bool AutoDecompress 
        {
            get => HttpClientHandler.AutomaticDecompression > 0;
            set => HttpClientHandler.AutomaticDecompression = value ? DecompressionMethods.Deflate | DecompressionMethods.GZip : DecompressionMethods.None;
        }
        public bool SolidRequest { get; set; } = true;
        public int SolidRequestTimeoutInMilliseconds { get; set; } = 10*60*1000;
        public bool HasRange { get; set; } = false;
        public long RangeStart { get; set; } = 0;
        public long RangeEnd { get; set; } = long.MaxValue;

        public List<HttpStatusCode> ValidHttpStatus { get; set; } = new List<HttpStatusCode> { HttpStatusCode.Accepted, HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.PartialContent, HttpStatusCode.NonAuthoritativeInformation, HttpStatusCode.ResetContent  };

        public List<HttpStatusCode> BackOffHttpStatus { get; set; } = new List<HttpStatusCode> { HttpStatusCode.RequestTimeout, HttpStatusCode.BadGateway, HttpStatusCode.GatewayTimeout, HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable };

        public object RequestCallbackParameter { get; set; } = null;
        public object ErrorCallbackParameter { get; set; } = null;

        public WebParameters(Uri url, string uniquehttpclientkey, HttpClientHandler handler=null)
        {
            Url = url;
            Key = uniquehttpclientkey;
            Create(handler);
        }

        public Task PostProcessRequestAsync(WebStream w, CancellationToken token=default(CancellationToken))
        {
           return RequestCallback?.Invoke(w,RequestCallbackParameter,token) ?? Task.FromResult(0);
        }
        public Task<bool> ProcessErrorAsync(WebStream w, CancellationToken token = default(CancellationToken))
        {
            return ErrorCallback?.Invoke(w,ErrorCallbackParameter,token) ?? Task.FromResult(false);
        }

        public void Dispose()
        {
            lock (_httpClients)
            {
                _disposed = true;
                if (_httpClients.ContainsKey(Key))
                {
                    HttpClientInfo info = _httpClients[Key];
                    info.Count--;
                    if (info.Count == 0)
                    {
                        info.Client?.Dispose();
                        info.Client = null;
                        _httpClients.Remove(Key);
                    }
                }

            }
        }
        ~WebParameters()
        {
            if (!_disposed) //Make Sure we release the httpClientCount
                Dispose();
        }
    }
}
