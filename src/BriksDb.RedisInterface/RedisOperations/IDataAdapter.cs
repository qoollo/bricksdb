using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Request;

namespace BricksDb.RedisInterface.RedisOperations
{
    interface IDataAdapter
    {
        string Read(string key, out RequestDescription result);

        RequestDescription Create(string key, string value);
    }
}
