using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Proxy.ProxyNet
{
    internal interface IProxyNetModule
    {
        RemoteResult Process(ServerId server, InnerData ev);
        RemoteResult GetTransaction(ServerId server, UserTransaction transaction, out UserTransaction result);
    }
}
