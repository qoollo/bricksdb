using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BricksDb.RedisInterface.Server
{
    class ConfigurationHelper
    {
        private static volatile ConfigurationHelper _instance;
        private static readonly object SyncObj = new Object();
        public string DistributorHost;
        public int DistributorPort;
        public int CountThreads;

        public static ConfigurationHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_instance == null)
                            _instance = new ConfigurationHelper();
                    }
                }

                return _instance;
            }
        }

        public ConfigurationHelper()
        {
            var section = ConfigurationManager.GetSection("RedisBenchmark") as NameValueCollection;
            DistributorHost = section["distributorHost"];
            DistributorPort = int.Parse(section["distributorPort"]);
            CountThreads = int.Parse(section["countTreads"]);
        }
    }
}
