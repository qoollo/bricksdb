using System;
using Qoollo.Client.CollectorGate;

namespace Qoollo.Benchmark.Send
{
    interface IDataAdapter:IDisposable
    {
        void Start();
    }
}
