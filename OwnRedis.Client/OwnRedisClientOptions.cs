using System;
using System.Collections.Generic;
using System.Text;

namespace OwnRedis.Client
{
    public class OwnRedisClientOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(10);
    }
}
