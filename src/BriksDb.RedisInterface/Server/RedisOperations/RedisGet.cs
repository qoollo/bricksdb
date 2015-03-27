using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.Server.RedisOperations
{
    class RedisGet:RedisOperation
    {
        public RedisGet(IStorage<string, string> tableStorage) : base(tableStorage)
        {
        }
    }
}
