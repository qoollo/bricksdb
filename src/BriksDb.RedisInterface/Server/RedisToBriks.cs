using BricksDb.RedisInterface.BriksCommunication;
using Qoollo.Client.Configuration;
using Qoollo.Client.Support;

namespace BricksDb.RedisInterface.Server
{
    class RedisToBriks
    {
        private readonly RedisListener _redisListener;
        private readonly RedisGate _redisGate;

        public RedisToBriks()
        {
            _redisGate = new RedisGate(
                    new NetConfiguration(RedisListener.LocalIpAddress().ToString(), 8000, Consts.WcfServiceName),
                    new ProxyConfiguration(Consts.ChangeDistributorTimeoutSec),
                    new CommonConfiguration(ConfigurationHelper.Instance.CountThreads));

            _redisGate.Build();
            var processor = new RedisMessageProcessor(_redisGate.RedisTable);
            _redisListener = new RedisListener(processor, _redisGate.RedisTable);
        }

        public void Start()
        {
            _redisGate.Start();
            _redisListener.ListenWithQueueAsync();
        }

        public void Stop()
        {
            _redisListener.StopListen();
            _redisGate.Dispose();
        }
    }
}
