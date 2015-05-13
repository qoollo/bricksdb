using Qoollo.Client.Request;

namespace BricksDb.RedisInterface.RedisOperations
{
    class RedisGet : RedisOperation
    {
        private readonly IDataAdapter _dataAdapter;

        public RedisGet(IDataAdapter dataAdapter, string operationName)
            : base(operationName)
        {
            _dataAdapter = dataAdapter;
        }

        public override string PerformOperation(object parameterArray)
        {
            var parameters = parameterArray as string[]; // TODO: проверить на null
            var key = parameters[0];
            RequestDescription request;

            _dataAdapter.Read(key, out request);
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
