using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NutzCode.Libraries.Web
{
    public interface IStreamFactory
    {
        Task<WebStream> CreateStreamAsync(WebParameters pars, CancellationToken token = new CancellationToken());
        Task<string> GetUrlAsync(string url, Func<WebParameters, HttpClient> httpClientFactory, string postData, string encoding, string uagent = "", Dictionary<string, string> headers = null, CancellationToken token = default(CancellationToken));
        WebParameters CreateWebParameters(Uri uri, Func<WebParameters, HttpClient> httpClientFactory);
    }
}
