using System;
using System.Collections.Generic;
using System.Data;
using Qoollo.Impl.Common;
using Qoollo.Impl.Modules.Db.Impl;

namespace Qoollo.Impl.Writer.Db.Commands
{
    internal interface IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader>
    {
        bool CreateDb(TConnection connection);

        string GetKeyName();
        List<string> GetTableNameList();

        string GetKeyInitialization();
        void GetFieldsDescription(DataTable dataTable);

        TCommand Create(TKey key, TValue value);
        TCommand Update(TKey key, TValue value);
        TCommand Delete(TKey key);
        TCommand Read();

        TValue ReadObjectFromReader(DbReader<TReader> reader, out TKey key);
        TValue ReadObjectFromSearchData(List<Tuple<object, string>> fields);

        RemoteResult CustomOperation(TConnection connection, TKey key, byte[] value, string description);
        RemoteResult CustomOperationRollback(TConnection connection, TKey key, byte[] value, string description);        
    }
}
