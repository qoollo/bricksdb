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
        public RedisSet(IStorage<string, string> tableStorage)
            : base(tableStorage){}

        public override string PerformOperation(object parameter_array)
        {
            string[] parameters = parameter_array as string[]; // TODO: проверить на null
            var key = parameters[0];
            var value = parameters[1];
            var responseBriks = table.Create(key, value);
            if (responseBriks.IsError)
            {
                Interlocked.Increment(ref OperationFail);
            }
            else
            {
                Interlocked.Increment(ref OperationSuccess);
            }

            var responseRedis = "+OK\r\n"; // всегда ОК, чтобы бенчмарк работал. Ошибки считаются внутри этой системы
            return responseRedis;
        }

    }
}
