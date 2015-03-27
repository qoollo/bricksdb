using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BricksDb.RedisInterface.BriksCommunication;
using Qoollo.Client.Configuration;
using Qoollo.Client.Support;

namespace BricksDb.RedisInterface.Server
{
    class RedisToBriks
    {
        private RedisListener _redisListener;
        private RedisGate _redisGate;

        public RedisToBriks()
        {
            _redisListener = new RedisListener(ProcessMessage);
            _redisGate = new RedisGate(new NetConfiguration(RedisListener.LocalIPAddress().ToString(), 8000),
                new ProxyConfiguration(Consts.ChangeDistributorTimeoutSec), new CommonConfiguration(Consts.CountThreads));
        }

        public void StartServer()
        {
            _redisGate.Build();
            _redisListener.ListenWithQueue();
        }

        private string ProcessMessage(string message)
        {
            var responce = "+OK\r\n";
            return responce;
        }
    }
}
