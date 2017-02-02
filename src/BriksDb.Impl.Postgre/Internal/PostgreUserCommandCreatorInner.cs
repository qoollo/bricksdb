using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.Writer.Db.Commands;

namespace Qoollo.Impl.Postgre.Internal
{
    internal class PostgreUserCommandCreatorInner<TKey, TValue> : IUserCommandCreator<NpgsqlCommand, NpgsqlConnection, TKey, TValue, NpgsqlDataReader>
    {
        private readonly PostgreUserCommandCreator<TKey, TValue> _userCommandCreator;

        public PostgreUserCommandCreatorInner(PostgreUserCommandCreator<TKey, TValue> userCommandCreator)
        {
            _userCommandCreator = userCommandCreator;
        }

        public bool CreateDb(NpgsqlConnection connection)
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

        public NpgsqlCommand Create(TKey key, TValue value)
        {
            return _userCommandCreator.Create(key, value);
        }

        public NpgsqlCommand Update(TKey key, TValue value)
        {
            return _userCommandCreator.Update(key, value);
        }

        public NpgsqlCommand Delete(TKey key)
        {
            return _userCommandCreator.Delete(key);
        }

        public NpgsqlCommand Read()
        {
            return _userCommandCreator.Read();
        }

        public TValue ReadObjectFromReader(DbReader<NpgsqlDataReader> reader, out TKey key)
        {
            return _userCommandCreator.ReadObjectFromReader(reader.Reader, out key);
        }

        public TValue ReadObjectFromSearchData(List<Tuple<object, string>> fields)
        {
            return _userCommandCreator.ReadObjectFromSearchData(fields);
        }

        public RemoteResult CustomOperation(NpgsqlConnection connection, TKey key, byte[] value,
            string description)
        {
            var result = _userCommandCreator.CustomOperation(connection, key, value, description);
            if (result.IsSuccess)
                return new SuccessResult();
            return new InnerFailResult(result.Description);
        }

        public RemoteResult CustomOperationRollback(NpgsqlConnection connection, TKey key,
            byte[] value, string description)
        {
            var result = _userCommandCreator.CustomOperationRollback(connection, key, value, description);
            if (result.IsSuccess)
                return new SuccessResult();
            return new InnerFailResult(result.Description);
        }
    }
}