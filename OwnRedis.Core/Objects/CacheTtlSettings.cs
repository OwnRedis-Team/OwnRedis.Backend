using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Objects
{
    public class CacheTtlSettings
    {
        public TimeSpan RamToFallback { get; set; }
        public TimeSpan FallbackToDb { get; set; }
        public TimeSpan CleanupDelay { get; set; }
    }
}
