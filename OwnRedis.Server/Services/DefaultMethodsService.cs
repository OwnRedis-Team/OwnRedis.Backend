using Microsoft.Extensions.Options;
using OwnRedis.Core;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;
using OwnRedis.Server.Database;

public class DefaultMethodsService : ICacheMethodsService
{
    private readonly IDateTimeProvider _clock;
    private readonly ICacheTtlPolicy _ttlPolicy;
    private readonly IRamCacheStorage _ram;
    private readonly IFallbackCacheStorage _fallback;
    private readonly ICacheRepository _repository;
    private readonly ICacheSerializer _serializer;
    private readonly IOptions<CacheTtlSettings> _ttlSettings;

    public DefaultMethodsService(
        IDateTimeProvider clock,
        ICacheTtlPolicy ttlPolicy,
        IRamCacheStorage ram,
        IFallbackCacheStorage fallback,
        ICacheRepository repository,
        ICacheSerializer serializer,
        IOptions<CacheTtlSettings> ttlSettings)
    {
        _clock = clock;
        _ttlPolicy = ttlPolicy;
        _ram = ram;
        _fallback = fallback;
        _repository = repository;
        _serializer = serializer;
        _ttlSettings = ttlSettings;
    }

    public async Task<CacheObject?> GetAsync(string key)
    {
        var now = _clock.UtcNow;

        var fromRam = TryGetFromRam(key, now);
        if (fromRam != null) return fromRam;

        var fromFallback = TryGetFromFallback(key, now);
        if (fromFallback != null) return fromFallback;

        return await GetFromDatabaseAsync(key, now);
    }

    public async Task SetAsync(string key, CacheObject value, TimeSpan secondsTTL)
    {
        var now = _clock.UtcNow;

        SetInRam(key, value, now, secondsTTL);
        await SaveToDatabaseAsync(key, value, secondsTTL);
    }

    public async Task<CacheObject?> DeleteAsync(string key)
    {
        _ram.TryRemove(key, out var ramVal);
        _fallback.TryRemove(key, out _);
        await _repository.DeleteAsync(key);
        return ramVal;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (_ram.ContainsKey(key)) return true;

        var now = _clock.UtcNow;
        return await _repository.ExistsAsync(key, now);
    }

    public async Task<object> GetAdminStatsAsync()
    {
        var dbItems = await _repository.GetAllAsync();

        var allKeysMap = new Dictionary<string, (string Location, DateTimeOffset TTL)>();

        foreach (var item in dbItems)
            allKeysMap[item.Key] = ("Database", item.TTL);

        foreach (var kvp in _fallback.Keys)
            if (_fallback.TryGetValue(kvp, out var val))
                allKeysMap[kvp] = ("Fallback", val.TTL);

        foreach (var kvp in _ram.Keys)
            if (_ram.TryGetValue(kvp, out var val))
                allKeysMap[kvp] = ("RAM", val.TTL);

        var allKeysList = allKeysMap
            .Select(x => new { key = x.Key, location = x.Value.Location, ttl = x.Value.TTL })
            .OrderBy(x => x.key)
            .ToList();

        return new
        {
            storageCount = _ram.Count,
            fallbackCount = _fallback.Count,
            databaseCount = dbItems.Count,
            allKeys = allKeysList,
            logs = AdminLogger.Logs.ToArray().Reverse(),
            memoryUsage = AdminLogger.GetMemoryUsage()
        };
    }

    private CacheObject? TryGetFromRam(string key, DateTimeOffset now)
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

    private CacheObject? TryGetFromFallback(string key, DateTimeOffset now)
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

    private async Task<CacheObject?> GetFromDatabaseAsync(string key, DateTimeOffset now)
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

    private void SetInRam(string key, CacheObject value, DateTimeOffset now, TimeSpan ttl)
    {
        value.TTL = now + ttl;
        _ram.Set(key, value);
    }

    private async Task SaveToDatabaseAsync(string key, CacheObject value, TimeSpan ttl)
    {
        var dbTtl = value.TTL + _ttlSettings.Value.FallbackToDb;
        var jsonString = _serializer.Serialize(value.Value);
        await _repository.SaveAsync(key, jsonString, dbTtl, ttl);
        AdminLogger.Log($"Set: '{key}' (RAM: {ttl}s, DB запас до {dbTtl:T})");
    }
}