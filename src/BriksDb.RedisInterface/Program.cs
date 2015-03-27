using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BricksDb.RedisInterface.BriksCommunication;
using BricksDb.RedisInterface.Server;
using Qoollo.Client.Configuration;
using Qoollo.Client.Support;

namespace BricksDb.RedisInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new RedisToBriks();
            server.StartServer();
        }
    }
}
