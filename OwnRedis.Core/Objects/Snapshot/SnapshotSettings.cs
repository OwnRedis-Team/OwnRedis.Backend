using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Objects.Snapshot
{
    public class SnapshotSettings
    {
        public int IntervalSeconds { get; set; } = 300;
        public string FilePath { get; set; } = "cache_snapshot.json";
    }
}
