using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwnRedis.Core.Inrerfaces
{
    public interface ICacheSerializer
    {
        string Serialize(object value);
        object? Deserialize(string json);
    }
}
