using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OwnRedis.Core;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;
using OwnRedis.Server.Database;

namespace OwnRedis.Server.Services.TTL
{
    public class DatabaseCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<CacheTtlSettings> _ttlSettings;

        public DatabaseCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            IDateTimeProvider clock,
            IOptions<CacheTtlSettings> ttlSettings)
        {
            _scopeFactory = scopeFactory;
            _clock = clock;
            _ttlSettings = ttlSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = _clock.UtcNow;
                await CleanExpiredDatabaseItemsAsync(now, stoppingToken);
                await Task.Delay(_ttlSettings.Value.CleanupDelay, stoppingToken);
            }
        }

        private async Task CleanExpiredDatabaseItemsAsync(DateTimeOffset now, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RedisDbContext>();

                var expiredInDb = await db.CacheItems
                    .Where(x => x.TTL <= now)
                    .ToListAsync(stoppingToken);

                if (expiredInDb.Any())
                {
                    db.CacheItems.RemoveRange(expiredInDb);
                    await db.SaveChangesAsync(stoppingToken);
                    AdminLogger.Log($"Background DB: удалено {expiredInDb.Count} просроченных записей");
                }
            }
            catch (Exception ex)
            {
                AdminLogger.Log($"Ошибка чистки БД: {ex.Message}");
            }
        }
    }
}
