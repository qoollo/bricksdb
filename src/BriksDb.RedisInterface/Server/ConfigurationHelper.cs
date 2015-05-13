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
        public string Localhost;

        public string DbWriterHost;
        public int PortForDistributor;
        public int PortForCollector;
        public int CountThreadsWriter;
        public int CountReplicsWriter;
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
            ProxyConfiguration();
            DbWriterConfiguration();     
        }

        private void DbWriterConfiguration()
        {
            var section = ConfigurationManager.GetSection("DbWriter") as NameValueCollection;
            DbWriterHost = section["host"];
            PortForDistributor = int.Parse(section["portForDistributor"]);
            PortForCollector = int.Parse(section["portForCollector"]);
            CountThreadsWriter = int.Parse(section["countTreads"]);
            CountReplicsWriter = int.Parse(section["countReplics"]);
        }

        private void ProxyConfiguration()
        {
            var section = ConfigurationManager.GetSection("RedisBenchmark") as NameValueCollection;
            DistributorHost = section["distributorHost"];
            DistributorPort = int.Parse(section["distributorPort"]);
            CountThreads = int.Parse(section["countTreads"]);
            Localhost = section["localhost"];
        }
    }
}
