using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Objects.Snapshot
{
    public class SnapshotItem
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
        public DateTimeOffset TTL { get; set; }
    }
}
