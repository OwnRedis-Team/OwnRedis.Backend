using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Inrerfaces
{
    public interface IDateTimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }
}
