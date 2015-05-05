using System;
using System.Threading;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.Server.RedisOperations
{
    abstract class RedisOperation
    {
        protected IStorage<string, string> Table;
        private int _operationSuccess;
        private int _operationFail;
        public string OperationName { get; private set; }

        protected RedisOperation(IStorage<string,string> tableStorage, string operationName)
        {
            _operationSuccess = 0;
            _operationFail = 0;
            Table = tableStorage;
            OperationName = operationName;
        }

        public abstract string PerformOperation(object parameterArray);

        public void WritePerformanceToConsole()
        {
            Console.WriteLine("Operation {0}: Success # {1}, Fail # {2}", OperationName, _operationSuccess,
                _operationFail);
        }

        protected void Success()
        {
            Interlocked.Increment(ref _operationSuccess);
        }

        protected void Fail()
        {
            Interlocked.Increment(ref _operationFail);
        }
    }
}
