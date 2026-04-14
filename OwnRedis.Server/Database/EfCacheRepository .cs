using Microsoft.EntityFrameworkCore;

namespace OwnRedis.Server.Database
{
    public class EfCacheRepository : ICacheRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public EfCacheRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<DbCacheItem?> GetByKeyAsync(string key)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RedisDbContext>();
            return await db.CacheItems.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
        }

        public async Task SaveAsync(string key, string valueJson, DateTimeOffset ttl, TimeSpan originalTtl)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RedisDbContext>();

            var existing = await db.CacheItems.FirstOrDefaultAsync(x => x.Key == key);
            if (existing != null)
            {
                existing.ValueJson = valueJson;
                existing.TTL = ttl;
                existing.OriginalTTLSeconds = originalTtl.TotalSeconds;
            }
            else
            {
                await db.CacheItems.AddAsync(new DbCacheItem
                {
                    Key = key,
                    ValueJson = valueJson,
                    TTL = ttl,
                    OriginalTTLSeconds = originalTtl.TotalSeconds
                });
            }

            await db.SaveChangesAsync();
        }

        public async Task UpdateTtlAsync(string key, DateTimeOffset ttl)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RedisDbContext>();

            var item = await db.CacheItems.FirstOrDefaultAsync(x => x.Key == key);
            if (item != null)
            {
                item.TTL = ttl;
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(string key)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RedisDbContext>();

            var item = await db.CacheItems.FirstOrDefaultAsync(x => x.Key == key);
            if (item != null)
            {
                db.CacheItems.Remove(item);
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(string key, DateTimeOffset now)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RedisDbContext>();
            return await db.CacheItems.AnyAsync(x => x.Key == key && x.TTL > now);
        }

        public async Task<List<DbCacheItem>> GetAllAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RedisDbContext>();
            return await db.CacheItems.AsNoTracking().ToListAsync();
        }
    }
}
