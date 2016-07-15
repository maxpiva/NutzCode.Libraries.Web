using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NutzCode.Libraries.Web
{
    public class WebStreamFactory : IStreamFactory<WebParameters>
    {
        public static WebStreamFactory Instance { get; }=new WebStreamFactory();
        public async Task<WebStream> CreateStreamAsync(WebParameters pars, CancellationToken token = new CancellationToken())
        {
            return await WebStream.CreateStreamAsync<WebStream, WebParameters>(pars, token);
        }

        public async Task<string> GetUrlAsync(string url, string postData, string encoding, string uagent = "", Dictionary<string, string> headers = null)
        {
            WebParameters pars = new WebParameters(new Uri(url));
            return await WebStream.GetUrlAsync<WebStream, WebParameters>(pars, postData, encoding, uagent, headers);
        }
    }
}
