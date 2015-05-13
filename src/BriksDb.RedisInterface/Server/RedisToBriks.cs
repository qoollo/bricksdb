using System;
using BricksDb.RedisInterface.BriksCommunication;
using BricksDb.RedisInterface.Server.RedisOperations;
using Qoollo.Client.Configuration;
using Qoollo.Client.Support;

namespace BricksDb.RedisInterface.Server
{
    class RedisToBriks : RedisToSmthSystem
    {
        private readonly RedisGate _redisGate;

        public RedisToBriks()
        {
            _redisGate = new RedisGate(
                    new NetConfiguration(RedisListener.LocalIpAddress().ToString(), 8000, Consts.WcfServiceName),
                    new ProxyConfiguration(Consts.ChangeDistributorTimeoutSec),
                    new CommonConfiguration(ConfigurationHelper.Instance.CountThreads));

            _redisGate.Build();
        }

        private void ConnectToBriksDb()
        {
            var result = _redisGate.RedisTable.SayIAmHere(ConfigurationHelper.Instance.DistributorHost,
                ConfigurationHelper.Instance.DistributorPort);
            Console.WriteLine(result);
        }

        protected override void InnerBuild(RedisMessageProcessor processor)
        {
            processor.AddOperation("SET", new RedisSet(_redisGate.RedisTable, "SET"));
            processor.AddOperation("GET", new RedisGet(_redisGate.RedisTable, "GET"));
        }

        public override void Start()
        {
            ConnectToBriksDb();
            _redisGate.Start();
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            _redisGate.Dispose();
        }
    }
}
