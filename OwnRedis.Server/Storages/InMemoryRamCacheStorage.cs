using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;
using System.Collections.Concurrent;

namespace OwnRedis.Server.Storages
{
    public class InMemoryRamCacheStorage : IRamCacheStorage
    {
        private readonly ConcurrentDictionary<string, CacheObject> _cache = new();

        public bool TryGetValue(string key, out CacheObject value) => _cache.TryGetValue(key, out value);
        public bool TryRemove(string key, out CacheObject value) => _cache.TryRemove(key, out value);
        public void Set(string key, CacheObject value) => _cache[key] = value;
        public bool ContainsKey(string key) => _cache.ContainsKey(key);
        public IEnumerable<string> Keys => _cache.Keys;
        public int Count => _cache.Count;
    }
}
