namespace BricksDb.RedisInterface.RedisOperations
{
    class RedisSet : RedisOperation
    {
        private readonly IDataAdapter _dataAdapter;

        public RedisSet(IDataAdapter dataAdapter, string operationName)
            : base(operationName)
        {
            _dataAdapter = dataAdapter;
        }

        public override string PerformOperation(object parameterArray)
        {
            var parameters = parameterArray as string[]; // TODO: проверить на null
            var key = parameters[0];
            var value = parameters[1];
            var responseBriks = _dataAdapter.Create(key, value);
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
