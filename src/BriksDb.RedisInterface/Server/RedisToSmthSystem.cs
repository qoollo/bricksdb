using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BricksDb.RedisInterface.Server
{
    abstract class RedisToSmthSystem
    {
        private RedisListener _redisListener;

        public void Build()
        {
            var processor = new RedisMessageProcessor();
            InnerBuild(processor);

            _redisListener = new RedisListener(processor);
        }

        protected abstract void InnerBuild(RedisMessageProcessor processor);

        public virtual void Start()
        {
            _redisListener.ListenWithQueueAsync();
        }

        public virtual void Stop()
        {
            _redisListener.StopListen();
        }
    }
}
