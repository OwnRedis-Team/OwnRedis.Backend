using System.Text.Json.Serialization;

namespace OwnRedis.Core.Objects;

public class RequestObject
{
    public string Key { get; set; }
    public object Value { get; set; }

    [JsonConverter(typeof(SecondsTimeSpanConverter))]
    [JsonPropertyName("secondsTTL")]
    public TimeSpan SecondsTTL { get; set; }
}