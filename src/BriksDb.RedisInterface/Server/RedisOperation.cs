using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BricksDb.RedisInterface.Server
{
    class RedisOperation
    {
        public readonly Func<object, string> RedisFunc;
    }
}
