using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BricksDb.RedisInterface.Server;

namespace BricksDb.RedisInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new RedisServer();
            listener.ListenWithQueue();
        }
    }
}
