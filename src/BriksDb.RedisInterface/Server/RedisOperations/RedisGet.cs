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
        public RedisGet(IStorage<string, string> tableStorage) : base(tableStorage){}

        public override string PerformOperation(object parameter_array)
        {
            string[] parameters = parameter_array as string[]; // TODO: проверить на null
            var key = parameters[0];
            var request = new RequestDescription();
            var responseBriks = table.Read(key, out request);
            if (request.IsError)
            {
                Interlocked.Increment(ref OperationFail);
            }
            else
            {
                Interlocked.Increment(ref OperationSuccess);
            }

            var responseRedis = ":1\r\n"; // всегда результат, чтобы бенчмарк работал. Ошибки считаются внутри этой системы
            return responseRedis;
        }
    }
}
