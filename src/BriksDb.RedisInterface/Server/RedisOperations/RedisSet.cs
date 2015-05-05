using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.Server.RedisOperations
{
    class RedisSet: RedisOperation
    {
        public RedisSet(IStorage<string, string> tableStorage, string operationName)
            : base(tableStorage, operationName) { }

        public override string PerformOperation(object parameterArray)
        {
            var parameters = parameterArray as string[]; // TODO: проверить на null
            var key = parameters[0];
            var value = parameters[1];
            var responseBriks = Table.Create(key, value);
            if (responseBriks.IsError)
                Fail();
            else
                Success();

            const string responseRedis = "+OK\r\n";
                // всегда ОК, чтобы бенчмарк работал. Ошибки считаются внутри этой системы
            return responseRedis;
        }

    }
}
