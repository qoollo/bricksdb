using System;

namespace Qoollo.Impl.Modules.Net.ReceiveBehavior
{
    internal interface IReceiveBehavior<TReceive>:IDisposable
    {
        void Start();
    }
}