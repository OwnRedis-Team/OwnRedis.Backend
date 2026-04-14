using OwnRedis.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Inrerfaces
{
    public interface IRamCacheStorage
    {
        bool TryGetValue(string key, out CacheObject value);
        bool TryRemove(string key, out CacheObject value);
        void Set(string key, CacheObject value);
        bool ContainsKey(string key);
        IEnumerable<string> Keys { get; }
        int Count { get; }
    }
}
