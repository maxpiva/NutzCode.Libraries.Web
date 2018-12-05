using System;
using System.Net.Http;

namespace NutzCode.Libraries.Web
{
    public class SeekableWebParameters : WebParameters
    {
        public long InitialLength { get; set; }
        public string Key { get; private set; }
        public SeekableWebParameters(Uri url, Func<WebParameters, HttpClient> httpclientFactory, string uniquekey, long initialLength) : base(url, httpclientFactory)
        {
            InitialLength = initialLength;
            Key = uniquekey;
        }
        private SeekableWebParameters() 
        {

        }
        public override WebParameters Clone()
        {
            SeekableWebParameters n = new SeekableWebParameters();
            this.CopyTo(n);
            return n;
        }
    }
}
