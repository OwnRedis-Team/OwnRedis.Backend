using System;
using System.Collections.Generic;
using System.Text;

namespace OwnRedis.Client.Interfaces
{
    public interface IOwnRedisClient
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
        Task<bool> DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}
