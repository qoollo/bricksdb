using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NpgsqlTypes;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.Writer.Db.Commands;

namespace Qoollo.Impl.Postgre.Internal
{
    internal class PostgreMetaDataCommandCreator<TKey, TValue> : IMetaDataCommandCreator<NpgsqlCommand, NpgsqlDataReader>
    {
        private string _keyName;
        private string _userKeyName;
        private string _metaTableName = "MetaTable";

        private NpgsqlDbType _keyType;

        private readonly UserCommandsHandler<NpgsqlCommand, NpgsqlDbType, NpgsqlConnection, TKey, TValue, NpgsqlDataReader> _handler;

        public PostgreMetaDataCommandCreator(
            IUserCommandCreator<NpgsqlCommand, NpgsqlConnection, TKey, TValue, NpgsqlDataReader> userCommandCreator)
        {
            _handler =
                new UserCommandsHandler<NpgsqlCommand, NpgsqlDbType, NpgsqlConnection, TKey, TValue, NpgsqlDataReader>(
                    userCommandCreator, this);
        }

        private static int GetLocal(bool local)
        {
            return local ? 0 : 1;
        }

        private static bool GetLocalBack(int local)
        {
            return local == 0;
        }

        private static int IsDeleted(bool isdeleted)
        {
            return isdeleted ? 0 : 1;
        }

        public void SetKeyName(string keyName)
        {
            _keyName = "Meta_" + keyName;
            _userKeyName = keyName;
            var descr = _handler.GetFieldsDescription();
            _keyType = descr.Find(x => x.Item1 == keyName).Item3;
        }

        public void SetTableName(List<string> tableName)
        {
            _metaTableName = tableName.Aggregate(_metaTableName + "_", (current, result) => current + result + "_");
            _metaTableName = _metaTableName.Remove(_metaTableName.Length - 1);
        }

        public NpgsqlCommand InitMetaDataDb(string idInit)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand CreateMetaData(bool remote, string dataHash, object key)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand DeleteMetaData(object key)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand UpdateMetaData(bool local, object key)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand SetDataDeleted(object key)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand SetDataNotDeleted(object key)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand ReadMetaData(NpgsqlCommand userRead, object key)
        {
            throw new NotImplementedException();
        }

        public Tuple<MetaData, bool> ReadMetaDataFromReader(DbReader<NpgsqlDataReader> reader, bool readuserId = true)
        {
            throw new NotImplementedException();
        }

        public MetaData ReadMetaFromSearchData(SearchData data)
        {
            throw new NotImplementedException();
        }

        public string ReadWithDeleteAndLocal(bool isDelete, bool local)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand ReadWithDeleteAndLocalList(NpgsqlCommand userRead, bool isDelete, List<object> keys)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand ReadWithDelete(NpgsqlCommand userRead, bool isDelete, object key)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand ReadWithDeleteAndLocal(NpgsqlCommand userRead, bool isDelete, bool local)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand CreateSelectCommand(string script, FieldDescription idDescription, List<FieldDescription> userParameters)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand CreateSelectCommand(SelectDescription description)
        {
            throw new NotImplementedException();
        }

        public NpgsqlCommand CreateSelectCommand(NpgsqlCommand script, FieldDescription idDescription, List<FieldDescription> userParameters)
        {
            throw new NotImplementedException();
        }

        public List<Tuple<object, string>> SelectProcess(DbReader<NpgsqlDataReader> reader)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, Type> GetFieldsDescription()
        {
            throw new NotImplementedException();
        }

        public FieldDescription GetKeyDescription()
        {
            throw new NotImplementedException();
        }
    }
}