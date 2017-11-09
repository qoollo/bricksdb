using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Writer.Interfaces
{
    internal interface IInputModule
    {
        void Process(InnerData data);
        RemoteResult ProcessSync(InnerData data);
        RemoteResult ProcessSyncPackage(List<InnerData> datas);
        Task<RemoteResult> ProcessTaskBased(InnerData data);
        InnerData ReadOperation(InnerData data);
        void Rollback(InnerData data);
        Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description);
    }
}