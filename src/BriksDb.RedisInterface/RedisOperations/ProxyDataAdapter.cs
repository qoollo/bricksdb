using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;
using Qoollo.Client.Request;

namespace BricksDb.RedisInterface.RedisOperations
{
    class ProxyDataAdapter:IDataAdapter
    {
        private readonly IStorage<string, string> _redisTable;

        public ProxyDataAdapter(IStorage<string, string> redisTable)
        {
            _redisTable = redisTable;
        }

        public string Read(string key, out RequestDescription result)
        {
            return _redisTable.Read(key, out result);
        }

        public RequestDescription Create(string key, string value)
        {
            return _redisTable.Create(key, value);
        }
    }
}
