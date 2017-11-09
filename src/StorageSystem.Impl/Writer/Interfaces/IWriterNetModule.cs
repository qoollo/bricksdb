using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Writer.Interfaces
{
    internal interface IWriterNetModule
    {
        void ASendToDistributor(ServerId server, NetCommand command);
        bool ConnectToDistributor(ServerId server);
        bool ConnectToWriter(ServerId server);
        void PingDistributors(List<ServerId> servers);
        void PingWriter(List<ServerId> servers);
        Task<RemoteResult> ProcessAsync(ServerId server, InnerData data);
        RemoteResult ProcessSync(ServerId server, List<InnerData> datas);
        RemoteResult ProcessSync(ServerId server, InnerData data);
        RemoteResult SendToDistributor(NetCommand command);
        RemoteResult SendToDistributor(ServerId server, NetCommand command);
        RemoteResult SendToWriter(ServerId server, NetCommand command);
        void TransactionAnswer(ServerId server, Transaction transaction);

        List<ServerId> GetServersByType(Type type);
        void RemoveConnection(ServerId server);
    }
}