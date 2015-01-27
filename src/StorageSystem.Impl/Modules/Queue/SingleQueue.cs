using System;
using Qoollo.Turbo.Threading.QueueProcessing;

namespace Qoollo.Impl.Modules.Queue
{
    internal class SingleQueue<T> : ControlModule
    {
        private int _countProcessors;
        private int _elementCounts;
        private DeleageQueueAsyncProcessor<T> _queue;        
        private Action<T> _action;

        protected void Registrate(int countProcessors, int elemenetsCount, Action<T> action)
        {
            _countProcessors = countProcessors;
            _elementCounts = elemenetsCount;
            _action = action;
        }        

        public int Count { get { return _queue.ElementCount; } }

        public void Add(T element)
        {
            _queue.Add(element);
        }

        public override void Start()
        {
            if (_action != null)
            {
                if (_queue != null)
                    _queue.Dispose();
                _queue = new DeleageQueueAsyncProcessor<T>(_countProcessors, _elementCounts, "", (obj, token) => _action(obj));
                _queue.Start();
            }            
        }       

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall && _queue!=null)
                _queue.Dispose();

            base.Dispose(isUserCall);
        }
    }
}
