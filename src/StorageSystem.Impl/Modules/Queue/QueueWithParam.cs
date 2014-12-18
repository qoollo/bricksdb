using System;
using Qoollo.Impl.Configurations;

namespace Qoollo.Impl.Modules.Queue
{
    internal class QueueWithParam<T> : SingleQueue<T>
    {
        private Action<T> _action;
        private QueueConfiguration _configuration;

        public void Registrate(QueueConfiguration configuration, Action<T> action)
        {
            Registrate(configuration.ProcessotCount, configuration.MaxSizeQueue, action);
        }

        public void RegistrateWithStart(QueueConfiguration configuration, Action<T> action)
        {
            Registrate(configuration, action);
            Start();
        }

        public void Registrate(Action<T> action)
        {
            _action = action;
        }

        public void SetConfiguration(QueueConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void Start()
        {
            if (_configuration != null)
                Registrate(_configuration, _action);

            base.Start();
        }
    }
}
