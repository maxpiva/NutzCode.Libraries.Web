using System;
using System.Threading;
using System.Threading.Tasks;

namespace NutzCode.Libraries.Web.StreamProvider
{
    public class WebDataProvider : IDisposable
    {
        public int MaxStreams { get; }
        public int BlockSize { get;  }        
        public int MaxBlockDistance { get; }


        private StreamLocker _streamLocker;
        private bool _disposed;
        private LRUCache<string, byte[]> _cache;

        public WebDataProvider(int maxStreams, int blocksize, int maxBlockDistance=2, int numbuckets=1024)
        {
            MaxStreams = maxStreams;
            BlockSize = blocksize;
            _cache = new LRUCache<string, byte[]>(numbuckets);
            MaxBlockDistance = maxBlockDistance;
            _streamLocker=new StreamLocker(maxStreams);
        }


        public async Task<int> Read(string key, Func<long, SeekableWebParameters> webParameterResolver, long maxsize, long position, byte[] buffer, int offset, int length, CancellationToken token)
        {
            //TODO Cancel any read when disposing
            if (_disposed)
                throw new ObjectDisposedException("StreamManager");
            length = (int)Math.Min(maxsize - position, length);
            length = Math.Min(buffer.Length - offset, length);
            int cnt = 0;
            while (length > 0)
            {
                long blockposition = position / BlockSize;
                int blockoffset = (int) (position%BlockSize);
                string cachekey = key + "*" + blockposition;
                byte[] data=_cache[cachekey];
                if (data!=null)
                {
                    int dr = Math.Min(length, data.Length - blockoffset);
                    dr = Math.Min(buffer.Length - offset, dr);
                    Array.Copy(data, blockoffset, buffer, offset, dr);
                    length -= dr;
                    position += dr;
                    offset += dr;
                    cnt += dr;
                }
                else
                {
                    using (StreamInfo res = await _streamLocker.GetOrCreateActiveStream(key, blockposition, MaxBlockDistance,
                        async (tok) =>
                        {
                            SeekableWebParameters wb = webParameterResolver(blockposition*BlockSize);
                            return await WebStreamFactory.Instance.CreateStreamAsync(wb, tok);
                        },token))
                    {
                        if (!res.IsEmpty) // If res.isEmpty means someone else have the stream active, we try to get the data from the cache in the next pass.
                        {
                            do
                            {
                                
                                int reqsize = (int) Math.Min(res.Stream.ContentLength - res.Stream.Position, BlockSize);                                    
                                data = new byte[reqsize];
                                int roffset = 0;
                                while (reqsize > 0)
                                {
                                    int l;
                                    try
                                    {
                                        l = await res.Stream.ReadAsync(data, roffset, reqsize, token);
                                    }
                                    catch (Exception)
                                    {
                                        l = 0;
                                    }
                                    if (l == 0)
                                    {
                                        res.Faulted = true;
                                        reqsize = 0;
                                    }
                                    else
                                    {
                                        reqsize -= l;
                                        roffset += l;
                                    }
                                }
                                if (!res.Faulted)
                                {
                                    string ckey = key + "*" + res.CurrentBlock;
                                    _cache[ckey] = data;
                                    if (res.CurrentBlock == blockposition)
                                    {
                                        int dr = Math.Min(length, data.Length - blockoffset);
                                        dr = Math.Min(buffer.Length - offset, dr);
                                        Array.Copy(data, blockoffset, buffer, offset, dr);
                                        length -= dr;
                                        position += dr;
                                        offset += dr;
                                        cnt += dr;
                                    }
                                    res.CurrentBlock++;
                                }
                            } while (res.CurrentBlock <= blockposition && !res.Faulted);
                        }
                        else
                            await Task.Delay(20, token); //Wait 20 ms before checking again
                    }
                    /*
                    if (_streamLocker.IsActive(key, blockposition, MaxBlockDistance)) //TODO Add Timeout, this is a walking timebomb.
                        {
                            await Task.Delay(20, token);
                        }
                        else
                        {
                            StreamInfo res = _streamLocker.GetStreamAndMakeItActive(key, blockposition,
                                MaxBlockDistance);
                            if (res == null)
                            {
                                SeekableWebParameters wb = webParameterResolver(blockposition * BlockSize);
                                WebStream s = await WebStreamFactory.Instance.CreateStreamAsync(wb, token);
                                if ((s.StatusCode != HttpStatusCode.OK) && (s.StatusCode != HttpStatusCode.PartialContent))
                                {
                                    _streamLocker.RemoveActive(key, blockposition);
                                    throw new IOException("Http Status (" + s.StatusCode + ")");
                                }
                                res = _streamLocker.AddNewStreamAndMakeItActive(key, blockposition, s);
                            }
                            long keyblock = res.CurrentBlock;
                            bool isDisposed = false;
                            do
                            {
                                int reqsize = (int)Math.Min(res.Stream.ContentLength - res.Stream.Position, BlockSize);
                                data = new byte[reqsize];
                                int roffset = 0;
                                while (reqsize > 0)
                                {
                                    int l;
                                    try
                                    {
                                        l = await res.Stream.ReadAsync(data, roffset, reqsize, token);


                                    }
                                    catch (Exception)
                                    {
                                        l = 0;
                                    }
                                    if (l == 0)
                                    {
                                        //                                    retries--;
                                        _streamLocker.RemoveActive(res.File, keyblock);
                                        res.Stream.Dispose();
                                        isDisposed = true;
                                        reqsize = 0;
                                    }
                                    else
                                    {
                                        reqsize -= l;
                                        roffset += l;
                                    }
                                }
                                if (!isDisposed)
                                {
                                    string ckey = key + "*" + res.CurrentBlock;
                                    _cache[ckey] = data;
                                    if (res.CurrentBlock == blockposition)
                                    {
                                        int dr = Math.Min(length, data.Length - blockoffset);
                                        dr = Math.Min(buffer.Length - offset, dr);
                                        Array.Copy(data, blockoffset, buffer, offset, dr);
#if TRACE
                                Console.WriteLine("COPYING: " + cachekey + " OFFSET: " + position + " LENGTH: " + dr);
#endif
                                        length -= dr;
                                        position += dr;
                                        offset += dr;
                                        cnt += dr;
                                    }
#if TRACE
                            else
                            {
                                //Console.WriteLine("PREFETCH: " + cachekey + " OFFSET: " + nblock*BlockSize + " LENGTH: " + roffset);

                            }
#endif
                                    res.CurrentBlock++;
                                }
                            } while (res.CurrentBlock <= blockposition && !isDisposed);
                            if (!isDisposed)
                                _streamLocker.ReturnStreamAndMakeItInactive(keyblock, res);
                                */
                }
            }
            return cnt;
        }

        protected void Dispose(bool disposing)
        {
            _disposed = true;
            if (disposing)
            {
                _streamLocker.Dispose();
                _cache.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void CloseAndDisposeFile(string file)
        {
            _streamLocker.RemoveFile(file);
        }
        ~WebDataProvider()
        {
            Dispose(false);
        }
    }
}
