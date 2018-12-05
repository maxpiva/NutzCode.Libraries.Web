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
    public class WebParameters
    {
        public virtual HttpClient HttpClient { get; } 
        public virtual HttpClientHandler HttpClientHandler { get; }
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

        public WebParameters(Uri url, Func<WebParameters, HttpClient> httpClientFactory, HttpClientHandler handler=null)
        {
            Url = url;
            HttpClientHandler = handler ?? new HttpClientHandler() { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip, MaxConnectionsPerServer = 48 };
            HttpClient = httpClientFactory(this);
            handler.UseCookies = false; //Custom Handling
        }

        internal WebParameters()
        {

        }
        public virtual WebParameters Clone()
        {
            WebParameters n=new WebParameters();
            this.CopyTo(n);
            return n;
        }


        public Task PostProcessRequestAsync(WebStream w, CancellationToken token=default(CancellationToken))
        {
           return RequestCallback?.Invoke(w,RequestCallbackParameter,token) ?? Task.FromResult(0);
        }
        public Task<bool> ProcessErrorAsync(WebStream w, CancellationToken token = default(CancellationToken))
        {
            return ErrorCallback?.Invoke(w,ErrorCallbackParameter,token) ?? Task.FromResult(false);
        }

    }
}
