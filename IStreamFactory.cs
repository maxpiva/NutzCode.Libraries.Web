using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NutzCode.Libraries.Web
{
    public interface IStreamFactory<T> where T : WebParameters
    {
        Task<WebStream> CreateStreamAsync(T pars, CancellationToken token = new CancellationToken());
        Task<string> GetUrlAsync(string url, string postData, string encoding, string uagent = "", Dictionary<string, string> headers = null);
        T CreateWebParameters(Uri uri);
    }
}
