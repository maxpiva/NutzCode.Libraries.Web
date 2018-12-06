using System;
using System.Net.Http;

namespace NutzCode.Libraries.Web
{
    public class SeekableWebParameters : WebParameters
    {

        public long InitialLength { get; set; }

        public SeekableWebParameters(Uri url, string uniquekey, long initialLength) : base(url, uniquekey)
        {
            InitialLength = initialLength;
        }

    }
}
