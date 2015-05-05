using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;
using Qoollo.Client.Request;

namespace BricksDb.RedisInterface.Server.RedisOperations
{
    class RedisGet:RedisOperation
    {
        public RedisGet(IStorage<string, string> tableStorage, string operationName) 
            : base(tableStorage, operationName){}

        public override string PerformOperation(object parameterArray)
        {
            var parameters = parameterArray as string[]; // TODO: проверить на null
            var key = parameters[0];
            RequestDescription request;
            
            Table.Read(key, out request);
            if (request.IsError)
                Fail();
            else
                Success();

            const string responseRedis = ":1\r\n";
                // всегда результат, чтобы бенчмарк работал. Ошибки считаются внутри этой системы
            return responseRedis;
        }
    }
}
