using OwnRedis.Client.Interfaces;
using OwnRedis.Core.Objects;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace OwnRedis.Client
{
    public class OwnRedisClient : IOwnRedisClient
    {
        private readonly HttpClient _httpClient;
        private readonly OwnRedisClientOptions _options;

        public OwnRedisClient(HttpClient httpClient, OwnRedisClientOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var response = await _httpClient.GetAsync($"/api/cache/{key}");
            if (!response.IsSuccessStatusCode) return default;

            //Get возвращает CacheObject
            var cacheObj = await response.Content.ReadFromJsonAsync<CacheObject>();

            // Десериализуем Value в нужный тип T
            if (cacheObj?.Value == null) return default;

            // Учитывая JsonCacheSerializer, Value - это JsonElement
            var json = cacheObj.Value.ToString();
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            var request = new RequestObject
            {
                Key = key,
                Value = value,
                SecondsTTL = ttl ?? _options.DefaultTtl
            };

            var response = await _httpClient.PostAsJsonAsync("/api/cache", request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var response = await _httpClient.DeleteAsync($"/api/cache/{key}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var response = await _httpClient.GetAsync($"/api/cache/exists/{key}");
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<ExistsResponse>();
            return result?.Exists ?? false;
        }

        private class ExistsResponse { public bool Exists { get; set; } }
    }
}
