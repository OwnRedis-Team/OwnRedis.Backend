using OwnRedis.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Inrerfaces
{
    public interface ICacheMethodsHelper
    {
        public CacheObject? TryGetFromRam(string key, DateTimeOffset now);
        public CacheObject? TryGetFromFallback(string key, DateTimeOffset now);
        public Task<CacheObject?> GetFromDatabaseAsync(string key, DateTimeOffset now);
        public void SetInRam(string key, CacheObject value, DateTimeOffset now, TimeSpan ttl);
        public Task SaveToDatabaseAsync(string key, CacheObject value, TimeSpan ttl);
    }
}
