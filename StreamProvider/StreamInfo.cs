
namespace NutzCode.Libraries.Web.StreamProvider
{
    public class StreamInfo
    {
        public string File { get; }
        public WebStream Stream { get; }
        public long StartBlock { get; }
        public long CurrentBlock { get; set; }

        public StreamInfo(string file, WebStream stream, long block)
        {
            File = file;
            Stream = stream;
            StartBlock = CurrentBlock = block;
        }
    }
}
