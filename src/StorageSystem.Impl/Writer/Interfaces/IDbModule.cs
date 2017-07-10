using System.Collections.Generic;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Impl.Writer.Interfaces
{
    internal interface IDbModule
    {
        string TableName { get; }

        RemoteResult InitDb();

        RemoteResult Create(InnerData obj, bool local);

        RemoteResult Update(InnerData obj, bool local);

        RemoteResult Delete(InnerData obj);

        RemoteResult DeleteFull(InnerData obj);

        RemoteResult AsyncProcess(RestoreDataContainer restoreData);

        RemoteResult SelectRead(SelectDescription description, out SelectSearchResult searchResult);

        RemoteResult RestoreUpdate(InnerData obj, bool local);

        RemoteResult RestoreUpdatePackage(List<InnerData> obj);

        RemoteResult CustomOperation(InnerData obj, bool local);

        InnerData ReadExternal(InnerData obj);

        RemoteResult CreateRollback(InnerData obj, bool local);

        RemoteResult UpdateRollback(InnerData obj, bool local);

        RemoteResult DeleteRollback(InnerData obj, bool local);

        RemoteResult CustomOperationRollback(InnerData obj, bool local);
    }
}