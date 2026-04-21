using Microsoft.Extensions.Options;
using OwnRedis.Core;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;
using OwnRedis.Server.Database;

namespace OwnRedis.Server.Services
{
    public class DefaultMethodsHelper : ICacheMethodsHelper
    {
        private readonly ICacheTtlPolicy _ttlPolicy;
        private readonly IRamCacheStorage _ram;
        private readonly IFallbackCacheStorage _fallback;
        private readonly ICacheRepository _repository;
        private readonly ICacheSerializer _serializer;
        private readonly IOptions<CacheTtlSettings> _ttlSettings;

        public DefaultMethodsHelper(
            IDateTimeProvider clock,
            ICacheTtlPolicy ttlPolicy,
            IRamCacheStorage ram,
            IFallbackCacheStorage fallback,
            ICacheRepository repository,
            ICacheSerializer serializer,
            IOptions<CacheTtlSettings> ttlSettings)
        {
            _ttlPolicy = ttlPolicy;
            _ram = ram;
            _fallback = fallback;
            _repository = repository;
            _serializer = serializer;
            _ttlSettings = ttlSettings;
        }

        public CacheObject? TryGetFromRam(string key, DateTimeOffset now)
        {
            if (!_ram.TryGetValue(key, out var ramValue)) return null;

            if (ramValue.TTL > now) return ramValue;

            if (_ram.TryRemove(key, out var expiredValue))
            {
                expiredValue.TTL = _ttlPolicy.GetFallbackTtl(now);
                _fallback.Set(key, expiredValue);
                AdminLogger.Log($"TTL истек: '{key}' перемещен в Fallback");
            }

            return null;
        }

        public CacheObject? TryGetFromFallback(string key, DateTimeOffset now)
        {
            if (!_fallback.TryGetValue(key, out var fallbackValue)) return null;

            if (fallbackValue.TTL > now)
            {
                _ram.Set(key, fallbackValue);
                return fallbackValue;
            }

            _fallback.TryRemove(key, out _);
            return null;
        }

        public async Task<CacheObject?> GetFromDatabaseAsync(string key, DateTimeOffset now)
        {
            var dbItem = await _repository.GetByKeyAsync(key);
            if (dbItem == null) return null;

            if (dbItem.TTL.ToUniversalTime() <= now)
            {
                await _repository.DeleteAsync(key);
                AdminLogger.Log($"Очистка: '{key}' окончательно удален из БД (время вышло)");
                return null;
            }

            var processedValue = _serializer.Deserialize(dbItem.ValueJson);
            var newRamTTL = now + TimeSpan.FromSeconds(dbItem.OriginalTTLSeconds);

            var cacheObj = new CacheObject
            {
                Value = processedValue ?? "empty",
                TTL = newRamTTL
            };

            _ram.Set(key, cacheObj);
            await _repository.UpdateTtlAsync(key, newRamTTL + _ttlSettings.Value.FallbackToDb);

            AdminLogger.Log($"Восстановление: '{key}' поднят из БД. Новый RAM TTL: {newRamTTL:T}");
            return cacheObj;
        }

        public void SetInRam(string key, CacheObject value, DateTimeOffset now, TimeSpan ttl)
        {
            value.TTL = now + ttl;
            _ram.Set(key, value);
        }

        public async Task SaveToDatabaseAsync(string key, CacheObject value, TimeSpan ttl)
        {
            var dbTtl = value.TTL + _ttlSettings.Value.FallbackToDb;
            var jsonString = _serializer.Serialize(value.Value);
            await _repository.SaveAsync(key, jsonString, dbTtl, ttl);
            AdminLogger.Log($"Set: '{key}' (RAM: {ttl}s, DB запас до {dbTtl:T})");
        }
    }
}
