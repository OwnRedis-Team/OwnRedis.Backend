using Microsoft.Extensions.Options;
using OwnRedis.Core;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;

namespace OwnRedis.Server.Services.TTL
{
    public class FallbackTtlBackgroundService : BackgroundService
    {
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<CacheTtlSettings> _ttlSettings;
        private readonly IFallbackCacheStorage _fallback;

        public FallbackTtlBackgroundService(
            IDateTimeProvider clock,
            IOptions<CacheTtlSettings> ttlSettings,
            IFallbackCacheStorage fallback)
        {
            _clock = clock;
            _ttlSettings = ttlSettings;
            _fallback = fallback;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = _clock.UtcNow;

                foreach (var key in _fallback.Keys.ToList())
                {
                    if (_fallback.TryGetValue(key, out var val) && now >= val.TTL)
                    {
                        _fallback.TryRemove(key, out _);
                        AdminLogger.Log($"Background Fallback: '{key}' удален из памяти");
                    }
                }

                await Task.Delay(_ttlSettings.Value.CleanupDelay, stoppingToken);
            }
        }
    }
}
