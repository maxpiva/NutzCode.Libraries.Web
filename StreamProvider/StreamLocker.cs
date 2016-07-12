using System;
using System.Linq;

namespace NutzCode.Libraries.Web.StreamProvider
{
    public class StreamLocker 
    {
        private ActiveStreamCache _activeStreams;
        private InactiveStreamsCache _inactiveStreams;
        private object _lock=new object();

        public StreamLocker(int maxInactiveStreams)
        {
            _inactiveStreams=new InactiveStreamsCache(maxInactiveStreams);
            _activeStreams= new ActiveStreamCache();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _activeStreams.Dispose();
                _inactiveStreams.Dispose();
            }
        }


        public WebStream RemoveActive(string file, long block)
        {
            lock (_lock)
            {
                return _activeStreams.Remove(file, block);
            }
        }

        public void RemoveFile(string file)
        {
            lock (_lock)
            {
                _activeStreams.RemoveAndDisposeKey(file);
                _inactiveStreams.RemoveAndDisposeKey(file);
            }
        }

        public bool IsActive(string file, long blockposition, int maxBlockDistance)
        {
            lock (_lock)
            {
                return _activeStreams.Keys.FirstOrDefault(a =>
                    a.Item1 == file && a.Item2 >= blockposition &&
                    a.Item2 <= blockposition + maxBlockDistance) != null;
            }
        }


        public StreamInfo GetStreamAndMakeItActive(string file, long blockposition, int maxBlockDistance)
        {
            lock (_lock)
            {
                Tuple<WebStream, long> n = _inactiveStreams.CheckAndRemove(file, blockposition, maxBlockDistance);
                if (n != null)
                {
                    _activeStreams[new Tuple<string, long>(file, n.Item2)] = n.Item1;
                    return new StreamInfo(file, n.Item1, n.Item2);
                }
                _activeStreams[new Tuple<string, long>(file, blockposition)] = null; //Lock the file/position 
                return null;
            }
        }




        public void ReturnStreamAndMakeItInactive(StreamInfo info)
        {
            lock (_lock)
            {
                _activeStreams.Remove(info.File, info.StartBlock);
                if (info.Stream.ContentLength != info.Stream.Position)
                    _inactiveStreams[Tuple.Create(info.File, info.CurrentBlock)] = info.Stream;
                else
                    info.Stream.Dispose();
            }

        }

        public StreamInfo AddNewStreamAndMakeItActive(string file, long block, WebStream w)
        {
            lock (_lock)
            {
                _activeStreams[Tuple.Create(file, block)] = w;
                return new StreamInfo(file, w, block);
            }
        }
    }
}
