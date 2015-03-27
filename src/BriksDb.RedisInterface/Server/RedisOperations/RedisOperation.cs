using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.Server
{
    abstract class RedisOperation
    {
        protected IStorage<string, string> table;
        protected int OperationSuccess;
        protected int OperationFail;
        protected string OperationName;

        public RedisOperation(IStorage<string,string> tableStorage)
        {
            OperationSuccess = 0;
            OperationFail = 0;
            table = tableStorage;
        }

        public virtual string PerformOperation(object parameters)
        {
            return null;
        }

        public void WritePerformanceToConsole()
        {
            Console.WriteLine("Operation {0}: Success # {1}, Fail # {2}", OperationName,OperationSuccess,OperationFail);
        }
    }
}
