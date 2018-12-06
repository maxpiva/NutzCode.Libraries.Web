using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NutzCode.Libraries.Web
{
    public class WebStreamFactory : IStreamFactory
    {
        public static WebStreamFactory Instance { get; }=new WebStreamFactory();
        public Task<WebStream> CreateStreamAsync(WebParameters pars, CancellationToken token = default(CancellationToken))
        {
            return WebStream.CreateStreamAsync<WebStream, WebParameters>(pars, token);
        }

        public Task<string> GetUrlAsync(string url, string uniqueHttpKey, string postData, string encoding, string uagent = "", Dictionary<string, string> headers = null, CancellationToken token = default(CancellationToken))
        {
            return WebStream.GetUrlAsync<WebStream, WebParameters>(CreateWebParameters(new Uri(url), uniqueHttpKey), postData, encoding, uagent, headers, token);
        }

        public WebParameters CreateWebParameters(Uri uri, string uniqueHttpKey)
        {
            return new WebParameters(uri, uniqueHttpKey);
        }
    }
}
