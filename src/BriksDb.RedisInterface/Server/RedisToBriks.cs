using System;
using BricksDb.RedisInterface.BriksCommunication;
using BricksDb.RedisInterface.RedisOperations;
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
                    new NetConfiguration(ConfigurationHelper.Instance.Localhost, 8000, Consts.WcfServiceName),
                    new ProxyConfiguration(Consts.ChangeDistributorTimeoutSec),
                    new CommonConfiguration(ConfigurationHelper.Instance.CountThreads));
        }

        protected override void InnerBuild(RedisMessageProcessor processor)
        {
            _redisGate.Build();

            processor.AddOperation("SET", new RedisSet(new ProxyDataAdapter(_redisGate.RedisTable), "SET"));
            processor.AddOperation("GET", new RedisGet(new ProxyDataAdapter(_redisGate.RedisTable), "GET"));
        }

        public override void Start()
        {
            _redisGate.Start();

            var result = _redisGate.RedisTable.SayIAmHere(ConfigurationHelper.Instance.DistributorHost,
                ConfigurationHelper.Instance.DistributorPort);
            Console.WriteLine(result);            

            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            _redisGate.Dispose();
        }
    }
}
