using OwnRedis.Core.Inrerfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core
{
    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
