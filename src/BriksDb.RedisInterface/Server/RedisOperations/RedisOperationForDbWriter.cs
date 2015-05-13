using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BricksDb.RedisInterface.Server.RedisOperations
{
    abstract class RedisOperationForDbWriter : RedisOperation
    {
        protected RedisOperationForDbWriter(string operationName)
            : base(operationName)
        {
        }
    }
}
