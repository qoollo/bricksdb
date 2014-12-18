using System;

namespace Qoollo.Impl.NetInterfaces
{
    internal interface ISingleConnection : IDisposable
    {
        bool Connect();
        void Start();
    }
}
