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
    private readonly ICacheMethodsHelper _helper;

    public DefaultMethodsService(
        IDateTimeProvider clock,
        ICacheTtlPolicy ttlPolicy,
        IRamCacheStorage ram,
        IFallbackCacheStorage fallback,
        ICacheRepository repository,
        ICacheSerializer serializer,
        IOptions<CacheTtlSettings> ttlSettings,
        ICacheMethodsHelper helper)
    {
        _clock = clock;
        _ttlPolicy = ttlPolicy;
        _ram = ram;
        _fallback = fallback;
        _repository = repository;
        _serializer = serializer;
        _ttlSettings = ttlSettings;
        _helper = helper;
    }

    public async Task<CacheObject?> GetAsync(string key)
    {
        var now = _clock.UtcNow;

        var fromRam = _helper.TryGetFromRam(key, now);
        if (fromRam != null) return fromRam;

        var fromFallback = _helper.TryGetFromFallback(key, now);
        if (fromFallback != null) return fromFallback;

        return await _helper.GetFromDatabaseAsync(key, now);
    }

    public async Task SetAsync(string key, CacheObject value, TimeSpan secondsTTL)
    {
        var now = _clock.UtcNow;

        _helper.SetInRam(key, value, now, secondsTTL);
        await _helper.SaveToDatabaseAsync(key, value, secondsTTL);
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
}