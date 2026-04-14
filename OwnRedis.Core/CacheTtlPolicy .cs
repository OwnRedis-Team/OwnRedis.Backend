using Microsoft.Extensions.Options;
using OwnRedis.Core.Inrerfaces;
using OwnRedis.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core
{
    public class CacheTtlPolicy : ICacheTtlPolicy
    {
        private readonly IOptions<CacheTtlSettings> _settings;

        public CacheTtlPolicy(IOptions<CacheTtlSettings> settings)
        {
            _settings = settings;
        }

        public DateTimeOffset GetRamTtl(DateTimeOffset now, TimeSpan ttl)
            => now + ttl;

        public DateTimeOffset GetFallbackTtl(DateTimeOffset now)
            => now + _settings.Value.RamToFallback;

        public DateTimeOffset GetDbTtl(DateTimeOffset ramTtl)
            => ramTtl + _settings.Value.FallbackToDb;
    }
}
