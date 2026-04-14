using OwnRedis.Core.Inrerfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace OwnRedis.Core
{
    public class JsonCacheSerializer : ICacheSerializer
    {
        private readonly JsonSerializerOptions _options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        public string Serialize(object value) => JsonSerializer.Serialize(value, _options);

        public object? Deserialize(string json)
        {
            var rawJson = JsonSerializer.Deserialize<JsonElement>(json);

            return rawJson.ValueKind switch
            {
                JsonValueKind.Array => JsonSerializer.Deserialize<List<object>>(json),
                JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(json),
                _ => JsonSerializer.Deserialize<object>(json)
            };
        }
    }
}
