using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.DbController.Db.Commands;
using Qoollo.Impl.Modules.Db.Impl;

namespace Qoollo.Impl.Sql.Internal
{
    internal class SqlUserCommandCreatorInner<TKey, TValue> : IUserCommandCreator<SqlCommand, SqlConnection, TKey, TValue, SqlDataReader>
    {
        private readonly SqlUserCommandCreator<TKey, TValue> _userCommandCreator;

        public SqlUserCommandCreatorInner(SqlUserCommandCreator<TKey, TValue> userCommandCreator)
        {
            _userCommandCreator = userCommandCreator;
        }

        public bool CreateDb(SqlConnection connection)
        {
            return _userCommandCreator.CreateDb(connection);
        }

        public string GetKeyName()
        {
            return _userCommandCreator.GetKeyName();
        }

        public List<string> GetTableNameList()
        {
            return _userCommandCreator.GetTableNameList();
        }        

        public string GetKeyInitialization()
        {
            return _userCommandCreator.GetKeyInitialization();
        }

        public void GetFieldsDescription(DataTable dataTable)
        {
            _userCommandCreator.GetFieldsDescription(dataTable);
        }

        public SqlCommand Create(TKey key, TValue value)
        {
            return _userCommandCreator.Create(key, value);
        }

        public SqlCommand Update(TKey key, TValue value)
        {
            return _userCommandCreator.Update(key, value);
        }

        public SqlCommand Delete(TKey key)
        {
            return _userCommandCreator.Delete(key);
        }

        public SqlCommand Read()
        {
            return _userCommandCreator.Read();
        }

        public TValue ReadObjectFromReader(DbReader<SqlDataReader> reader, out TKey key)
        {
            return _userCommandCreator.ReadObjectFromReader(reader.Reader, out key);
        }

        public TValue ReadObjectFromSearchData(List<Tuple<object, string>> fields)
        {
            return _userCommandCreator.ReadObjectFromSearchData(fields);
        }

        public RemoteResult CustomOperation(SqlConnection connection, TKey key, byte[] value,
            string description)
        {
            var result = _userCommandCreator.CustomOperation(connection, key, value, description);
            if (result.IsSuccess)
                return new SuccessResult();
            return new InnerFailResult(result.Description);
        }

        public RemoteResult CustomOperationRollback(SqlConnection connection, TKey key,
            byte[] value, string description)
        {
            var result = _userCommandCreator.CustomOperationRollback(connection, key, value, description);
            if (result.IsSuccess)
                return new SuccessResult();
            return new InnerFailResult(result.Description);
        }
    }
}
