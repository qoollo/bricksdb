using System;
using System.Collections.Generic;
using System.Data;
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

        public NpgsqlCommand SetKeytoCommand(NpgsqlCommand command, object key)
        {
            command.Parameters.Add("@" + _keyName, _keyType);
            command.Parameters["@" + _keyName].Value = key;

            return command;
        }

        public NpgsqlCommand InitMetaDataDb(string idInit)
        {
            return new NpgsqlCommand(string.Format("create table {1} ({0} primary key not null, " +
                                                "{2} integer not null, " +
                                                "{3} integer not null, " +
                                                "{4} timestamp, " +
                                                "{5} varchar (32)); " +
                                                "CREATE INDEX [SearchMetadata] ON {1} USING btree " +
                                                "({3}, {2}); "
                , idInit, _metaTableName, PostgreConsts.Local, PostgreConsts.IsDeleted, PostgreConsts.DeleteTime, PostgreConsts.Hash));
        }

        public NpgsqlCommand CreateMetaData(bool remote, string dataHash, object key)
        {
            var command = new NpgsqlCommand(string.Format("insert into {0} ({1}, {3}, {4}, {5}, {6})  " +
                                           "values (@{1}, {2}, 1, NULL, \'{7}\');",
                _metaTableName, _keyName, GetLocal(remote),
                PostgreConsts.Local, PostgreConsts.IsDeleted, PostgreConsts.DeleteTime, PostgreConsts.Hash, dataHash));
            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand DeleteMetaData(object key)
        {
            var command = new NpgsqlCommand(string.Format("delete from {0} " +
                                               "where {1} = @{1};", _metaTableName, _keyName));
            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand UpdateMetaData(bool local, object key)
        {
            var command = new NpgsqlCommand(string.Format("update {0} " +
                                                          "set {3} = {1} " +
                                                          "where {2} = @{2};",
                _metaTableName, GetLocal(local), _keyName, PostgreConsts.Local));
            return SetKeytoCommand(command, key);
        }

        //TODO
        public NpgsqlCommand SetDataDeleted(object key)
        {
            var command = new NpgsqlCommand(string.Format("update {0} " +
                                                          "set {2} = 0," +
                                                          "{3} = @time " +
                                                          "where {1} = @{1};",
                _metaTableName, _keyName, PostgreConsts.IsDeleted, PostgreConsts.DeleteTime));

            command.Parameters.Add("@time", NpgsqlDbType.Timestamp);
            command.Parameters["@time"].Value = DateTime.Now.ToString("u");
            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand SetDataNotDeleted(object key)
        {
            var command = new NpgsqlCommand(string.Format("update {0} " +
                                                          "set {2} = 1 " +
                                                          "where {1} = @{1};",
                _metaTableName, _keyName, PostgreConsts.IsDeleted));

            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand ReadMetaData(NpgsqlCommand userRead, object key)
        {
            var command = new NpgsqlCommand(
                string.Format(
                    "select {0}.{3}, {0}.{4}, {0}.{5}, {0}.{1} as MetaId, HelpTable.\"{6}\" as UserId, {0}.{7} " +
                    " from ( {2} ) as HelpTable right join {0} on HelpTable.\"{6}\" = {0}.{1} " +
                    " where {0}.{1} = @{1}",
                    _metaTableName, _keyName, userRead.CommandText, PostgreConsts.Local, PostgreConsts.IsDeleted,
                    PostgreConsts.DeleteTime, _userKeyName, PostgreConsts.Hash));

            return SetKeytoCommand(command, key);
        }

        //TODO
        public Tuple<MetaData, bool> ReadMetaDataFromReader(DbReader<NpgsqlDataReader> reader, bool readuserId = true)
        {
            object local = reader.GetValue(PostgreConsts.Local);
            object isDeleted = reader.GetValue(PostgreConsts.IsDeleted);
            object deleteTime = reader.GetValue(PostgreConsts.DeleteTime);
            object hash = reader.GetValue(PostgreConsts.Hash);

            MetaData meta = null;

            if (!(local is DBNull || isDeleted is DBNull || deleteTime is DBNull))
            {
                bool l = GetLocalBack((int)local);
                bool i = GetLocalBack((int)isDeleted);
                var t = (DateTime)deleteTime;

                meta = new MetaData(l, t, i, (string)hash);
            }

            bool data = false;
            if (readuserId)
            {
                object id = reader.GetValue("UserId");
                data = id is DBNull;
            }

            return new Tuple<MetaData, bool>(meta, data);
        }

        //TODO
        public MetaData ReadMetaFromSearchData(SearchData data)
        {
            object local = data.Fields.Find(x => x.Item2.ToLower() == PostgreConsts.Local.ToLower()).Item1;
            object isDeleted = data.Fields.Find(x => x.Item2.ToLower() == PostgreConsts.IsDeleted.ToLower()).Item1;
            object deleteTime = data.Fields.Find(x => x.Item2.ToLower() == PostgreConsts.DeleteTime.ToLower()).Item1;
            object hash = data.Fields.Find(x => x.Item2.ToLower() == PostgreConsts.Hash.ToLower()).Item1;
            object id = data.Fields.Find(x => x.Item2.ToLower() == _keyName.ToLower()).Item1;

            MetaData meta = null;

            if (!(local is DBNull || isDeleted is DBNull || deleteTime is DBNull))
            {
                bool l = GetLocalBack((int)local);
                bool i = GetLocalBack((int)isDeleted);
                var t = (DateTime)deleteTime;

                meta = new MetaData(l, t, i, (string)hash)
                {
                    Id = id
                };
            }

            return meta;
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
            var command = new NpgsqlCommand(string.Format("select * from ( {0} ) as MetaHelpTable " +
                                                          " inner join {1} on MetaHelpTable.{5} = {1}.{2}" +
                                                          " where {1}.{2} = @{2} and {1}.{4} = {3}",
                userRead.CommandText, _metaTableName, _keyName, IsDeleted(isDelete), PostgreConsts.IsDeleted,
                _userKeyName));

            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand ReadWithDeleteAndLocal(NpgsqlCommand userRead, bool isDelete, bool local)
        {
            if (local)
                return new NpgsqlCommand(string.Format("select * from ( {0} ) as MetaHelpTable " +
                                                       " inner join {1} on MetaHelpTable.{5} = {1}.{2}" +
                                                       " where {1}.{4} = {3}" +
                                                       " order by {2}",
                    userRead.CommandText, _metaTableName, _keyName, IsDeleted(isDelete), PostgreConsts.IsDeleted,
                    _userKeyName));

            return new NpgsqlCommand(string.Format("select * from ( {0} ) as MetaHelpTable " +
                                                   " inner join {1} on MetaHelpTable.{7} = {1}.{2}" +
                                                   " where {1}.{5} = {4} and {1}.{6} = {3}" +
                                                   " order by {2}",
                userRead.CommandText, _metaTableName, _keyName, IsDeleted(isDelete), GetLocal(false),
                PostgreConsts.Local, PostgreConsts.IsDeleted, _userKeyName));
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
            return new Dictionary<string, Type>
            {
                {PostgreConsts.Local.ToLower(), typeof (int)},
                {PostgreConsts.IsDeleted.ToLower(), typeof (int)},
                {PostgreConsts.DeleteTime.ToLower(), typeof (string)},
                {PostgreConsts.Hash.ToLower(), typeof (string)}
            };
        }

        public FieldDescription GetKeyDescription()
        {
            var field = _handler.GetDbFieldsDescription().FirstOrDefault(x => x.Item1.ToLower() == _userKeyName.ToLower());

            var description = new FieldDescription(_keyName, field.Item2)
            {
                IsFirstAsk = true
            };
            return description;
        }
    }
}