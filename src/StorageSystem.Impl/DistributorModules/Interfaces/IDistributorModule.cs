using System.Collections.Generic;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.DistributorModules.Interfaces
{
    internal interface IDistributorModule
    {
        ServerId LocalForDb { get; }

        List<WriterDescription> GetDestination(InnerData data, bool needAllServers);
        bool IsSomethingHappendInSystem();
        void ProcessTransaction(Common.Data.TransactionTypes.Transaction transaction);
        RemoteResult SayIAmHereRemoteResult(ServerId destination);
        void ServerNotAvailable(ServerId server);

        TResult Execute<TValue, TResult>(TValue value)
            where TValue : class;
    }
}