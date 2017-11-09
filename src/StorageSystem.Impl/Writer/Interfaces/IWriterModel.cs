using System.Collections.Generic;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Writer.Interfaces
{
    internal interface IWriterModel
    {
        ServerId Local { get; }
        List<HashMapRecord> LocalMap { get; }
        List<ServerId> OtherServers { get; }
        List<ServerId> Servers { get; }

        IEnumerable<ServerId> GetDestination(string hash);
        List<HashMapRecord> GetHashMap(ServerId server);
        bool IsMine(string hash);
        string UpdateHashViaNet(List<HashMapRecord> map);
        void UpdateModel();
    }
}