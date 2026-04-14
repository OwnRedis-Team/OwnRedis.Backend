using Microsoft.Extensions.Options;
using OwnRedis.Core;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;

namespace OwnRedis.Server.Services.TTL
{
    public class RamTtlBackgroundService : BackgroundService
    {
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<CacheTtlSettings> _ttlSettings;
        private readonly IRamCacheStorage _ram;
        private readonly IFallbackCacheStorage _fallback;

        public RamTtlBackgroundService(
            IDateTimeProvider clock,
            IOptions<CacheTtlSettings> ttlSettings,
            IRamCacheStorage ram,
            IFallbackCacheStorage fallback)
        {
            _clock = clock;
            _ttlSettings = ttlSettings;
            _ram = ram;
            _fallback = fallback;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = _clock.UtcNow;

                foreach (var key in _ram.Keys.ToList())
                {
                    if (_ram.TryGetValue(key, out var val) && now >= val.TTL)
                    {
                        if (_ram.TryRemove(key, out _))
                        {
                            val.TTL = now + _ttlSettings.Value.RamToFallback;
                            _fallback.Set(key, val);
                            AdminLogger.Log($"Background RAM: '{key}' -> Fallback");
                        }
                    }
                }

                await Task.Delay(_ttlSettings.Value.CleanupDelay, stoppingToken);
            }
        }
    }
}
