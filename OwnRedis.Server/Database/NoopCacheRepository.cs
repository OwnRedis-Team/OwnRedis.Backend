namespace OwnRedis.Server.Database
{
    public class NoopCacheRepository : ICacheRepository
    {
        public Task<DbCacheItem?> GetByKeyAsync(string key)
            => Task.FromResult<DbCacheItem?>(null);

        public Task SaveAsync(string key, string valueJson, DateTimeOffset ttl, TimeSpan originalTtl)
            => Task.CompletedTask;

        public Task UpdateTtlAsync(string key, DateTimeOffset ttl)
            => Task.CompletedTask;

        public Task DeleteAsync(string key)
            => Task.CompletedTask;

        public Task<bool> ExistsAsync(string key, DateTimeOffset now)
            => Task.FromResult(false);

        public Task<List<DbCacheItem>> GetAllAsync()
            => Task.FromResult(new List<DbCacheItem>());
    }
}
