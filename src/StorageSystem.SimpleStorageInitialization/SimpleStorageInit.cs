using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Libs.Logger;
using StorageSystem.Client.Configuration;
using StorageSystem.Client.DistributorGate;
using StorageSystem.Client.StorageGate;
using StorageSystem.Client.Support;

namespace StorageSystem.SimpleStorageInitialization
{
    public static class SimpleStorageInit<TKey, TValue>
    {
        public static void Start(List<DbFactory> factory,  string host, int port, int countReplics)
        {
            var net = new StorageNetConfiguration( host, port, port + 123, "testService", 10);
            var st = new StorageConfiguration("ServersFile", countReplics, 10, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60), false);
            var common = new CommonConfiguration(4, 10000);
            var timeout = new TimeoutConfiguration(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000));
            var storage = new StorageApi(net, st, common,timeout);

            Wait(storage, factory);
        }

        public static void Start(IDataProvider<TKey, TValue> dataProvider, List<DbFactory> factory, string[] args = null)
        {
            Tuple<string, int, int> tup = args == null ? AskParams() : ReadParams(args);

            var net = new StorageNetConfiguration( tup.Item1, tup.Item2, tup.Item2 + 123, "testService", 10);
            var st = new StorageConfiguration("ServersFile", tup.Item3, 10, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60), false);
            var common = new CommonConfiguration(4, 10000);
            var timeout = new TimeoutConfiguration(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000));
            var storage = new StorageApi(net, st, common, timeout);

            Wait(storage, factory);
        }

        private static void Wait(StorageApi storage, List<DbFactory> factory)
        {
            var logger = LoggerFactory.CreateLoggerFromAppConfig("StorageAppMain", "StorageApp",
               "LoggerConfigurationSection");
            Logger.InitializeLoggerInAssembly(logger, typeof(DistributorApi).Assembly);          

            try
            {
                storage.Build();

                factory.ForEach(x => storage.AddDbModule(x));          

                storage.Start();

                Console.WriteLine("Press enter to stop");
                Console.ReadLine();

            }
            catch (Exception e)
            {
                logger.Error(e, "");
                Console.ReadLine();
            }

            storage.Dispose();            
        }

        private static Tuple<string, int, int> ReadParams(string[] args)
        {
            return new Tuple<string, int, int>(args[0],
                int.Parse(args[1]), int.Parse(args[2]));
        }

        private static Tuple<string, int, int> AskParams()
        {
            Console.WriteLine("Input host:");
            string host = Console.ReadLine();
            Console.WriteLine("Input port:");
            int p1 = int.Parse(Console.ReadLine());
            Console.WriteLine("Input count replics:");
            int count = int.Parse(Console.ReadLine());

            return new Tuple<string, int, int>(host, p1, count);
        }
    }
}
