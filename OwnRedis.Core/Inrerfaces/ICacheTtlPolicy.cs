using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Inrerfaces
{
    public interface ICacheTtlPolicy
    {
        DateTimeOffset GetRamTtl(DateTimeOffset now, TimeSpan ttl);
        DateTimeOffset GetFallbackTtl(DateTimeOffset now);
        DateTimeOffset GetDbTtl(DateTimeOffset ramTtl);
    }
}
