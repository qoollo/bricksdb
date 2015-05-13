using System;
using BricksDb.RedisInterface.Server;

namespace BricksDb.RedisInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new RedisToBriks();
            server.Build();
            server.Start();
            Console.WriteLine("Press enter to stop");
            Console.ReadLine();
            server.Stop();
        }
    }
}
