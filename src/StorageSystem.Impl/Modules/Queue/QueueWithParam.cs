using System;
using Qoollo.Impl.Configurations;

namespace Qoollo.Impl.Modules.Queue
{
    internal class QueueWithParam<T> : SingleQueue<T>
    {
        private Action<T> _action;
        private readonly SingleQueueConfiguration _configuration;

        public QueueWithParam(string name, SingleQueueConfiguration configuration):base(name)
        {
            _configuration = configuration;
        }

        //public void Registrate(QueueConfiguration configuration, Action<T> action)
        //{
        //    Registrate(configuration.ProcessotCount, configuration.MaxSizeQueue, action);
        //}

        public void RegistrateWithStart( Action<T> action)
        {
            Registrate(action);
            Start();
        }

        public void Registrate(Action<T> action)
        {
            _action = action;
        }

        public override void Start()
        {
            Registrate(_configuration.CountThreads, _configuration.MaxSize, _action);

            base.Start();
        }
    }
}
