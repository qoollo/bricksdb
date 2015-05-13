using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.Server.RedisOperations
{
    abstract class RedisOperationForProxy : RedisOperation
    {
        protected IStorage<string, string> Table;

        protected RedisOperationForProxy(IStorage<string, string> tableStorage, string operationName)
            : base(operationName)
        {
            Table = tableStorage;
        }
    }
}
