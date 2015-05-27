using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Benchmark
{
    internal class ReturnValue<T>
    {
        public ReturnValue(T value)
        {
            _value = value;
            IsValueExist = true;
        }

        public ReturnValue()
        {
            IsValueExist = false;
        }

        private readonly T _value;
        public T Value { get { return _value; } }
        public bool IsValueExist { get; private set; }
    }

    class BlockingQueue<T>
    {
        public BlockingQueue(IEnumerable<T> collection)
        {
            Contract.Requires(collection != null);
            _queue = new Queue<T>(collection);
        }

        private readonly Queue<T> _queue;
        private readonly object _lock = new object();

        public void Enqueue(T element)
        {
            lock (_lock)
            {
                _queue.Enqueue(element);
            }
        }

        public ReturnValue<T> Dequeue()
        {
            var element = default (T);
            bool hasElements;
            lock (_lock)
            {
                hasElements = _queue.Count != 0;
                if(hasElements)
                    element = _queue.Dequeue();
            }
            return hasElements ? new ReturnValue<T>(element) : new ReturnValue<T>();
        }
    }
}
