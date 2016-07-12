

namespace NutzCode.Libraries.Web.StreamProvider
{
    // ReSharper disable once InconsistentNaming
    internal class LRUCacheItem<T,TS>
    {
        public LRUCacheItem(T k, TS v)
        {
            Key = k;
            Value = v;
        }
        public readonly T Key;
        public readonly TS Value;
    }
}
