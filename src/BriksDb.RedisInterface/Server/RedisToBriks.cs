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
        private RedisMessageProcessor _processor;

        public RedisToBriks()
        {
            _redisGate =
                new RedisGate(
                    new NetConfiguration(RedisListener.LocalIPAddress().ToString(), 8000, Consts.WcfServiceName),
                    new ProxyConfiguration(Consts.ChangeDistributorTimeoutSec),
                    new CommonConfiguration(Consts.CountThreads));
            _redisGate.Build();
            _processor = new RedisMessageProcessor(_redisGate.RedisTable);
            _redisListener = new RedisListener(_processor.ProcessMessage);
        }

        public void StartServer()
        {
            _redisGate.Start();
            _redisListener.ListenWithQueue();
        }

        
    }
}
