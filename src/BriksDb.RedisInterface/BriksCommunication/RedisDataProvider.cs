using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Support;

namespace BricksDb.RedisInterface.BriksCommunication
{
    class RedisDataProvider: CommonDataProvider<string,string>
    {
        public override string CalculateHashFromKey(string key)
        {
            return key.GetHashCode().ToString();
        }
    }
}
