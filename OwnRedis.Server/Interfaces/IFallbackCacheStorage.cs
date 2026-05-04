using OwnRedis.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Inrerfaces
{
    public interface IFallbackCacheStorage
    {
        bool TryGetValue(string key, out CacheObject value);
        bool TryRemove(string key, out CacheObject value);
        void Set(string key, CacheObject value);
        IEnumerable<string> Keys { get; }
        int Count { get; }
    }
}
