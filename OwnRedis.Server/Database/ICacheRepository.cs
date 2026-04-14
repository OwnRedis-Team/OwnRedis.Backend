using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Server.Database
{
    public interface ICacheRepository
    {
        Task<DbCacheItem?> GetByKeyAsync(string key);
        Task SaveAsync(string key, string valueJson, DateTimeOffset ttl, TimeSpan originalTtl);
        Task UpdateTtlAsync(string key, DateTimeOffset ttl);
        Task DeleteAsync(string key);
        Task<bool> ExistsAsync(string key, DateTimeOffset now);
        Task<List<DbCacheItem>> GetAllAsync();
    }
}
