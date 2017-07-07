using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Writer.Interfaces
{
    internal interface IMainLogicModule
    {
        RemoteResult Process(InnerData data);
        RemoteResult ProcessPackage(List<InnerData> datas);
        InnerData Read(InnerData data);
        void Rollback(InnerData data);
        Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description);
    }
}