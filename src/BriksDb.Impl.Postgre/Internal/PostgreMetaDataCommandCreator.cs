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
            var result = new NpgsqlCommand();
            result.CommandText = $@"CREATE TABLE {_metaTableName} 
                                    (
                                        {idInit} PRIMARY KEY NOT NULL,
                                        {PostgreConsts.Local} INTEGER NOT NULL,
                                        {PostgreConsts.IsDeleted} INTEGER NOT NULL,
                                        {PostgreConsts.DeleteTime} TIMESTAMP,
                                        {PostgreConsts.Hash} VARCHAR(32)
                                    );
                                    CREATE INDEX {_metaTableName + "_SearchMetadataIndex"} ON {_metaTableName} USING btree ({PostgreConsts.IsDeleted}, {PostgreConsts.Local});";

            return result;
        }

        public NpgsqlCommand CreateMetaData(bool remote, string dataHash, object key)
        {
            var result = new NpgsqlCommand();
            result.CommandText = 
                $@"INSERT INTO {_metaTableName} ({_keyName}, {PostgreConsts.Local}, {PostgreConsts.IsDeleted}, {PostgreConsts.DeleteTime}, {PostgreConsts.Hash})
                   VALUES                       (@{_keyName}, {GetLocal(remote)},   {IsDeleted(false)},        NULL,                      '{dataHash}')";

            return SetKeytoCommand(result, key);
        }

        public NpgsqlCommand DeleteMetaData(object key)
        {
            var command = new NpgsqlCommand($@"DELETE FROM {_metaTableName} 
                                               WHERE {_keyName} = @{_keyName};");
            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand UpdateMetaData(bool local, object key)
        {
            var command = new NpgsqlCommand($@"UPDATE {_metaTableName} 
                                               SET {PostgreConsts.Local} = {GetLocal(local)} 
                                               WHERE {_keyName} = @{_keyName};");
            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand SetDataDeleted(object key)
        {
            var command = new NpgsqlCommand($@"UPDATE {_metaTableName} 
                                               SET {PostgreConsts.IsDeleted} = {IsDeleted(true)},
                                                   {PostgreConsts.DeleteTime} = @time 
                                               WHERE {_keyName} = @{_keyName};");

            command.Parameters.Add("@time", NpgsqlDbType.Timestamp);
            command.Parameters["@time"].Value = DateTime.UtcNow.ToString("u");
            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand SetDataNotDeleted(object key)
        {
            var command = new NpgsqlCommand($@"UPDATE {_metaTableName} 
                                               SET {PostgreConsts.IsDeleted} = {IsDeleted(false)} 
                                               WHERE {_keyName} = @{_keyName};");

            return SetKeytoCommand(command, key);
        }

        public NpgsqlCommand ReadMetaData(NpgsqlCommand userRead, object key)
        {
            var result = new NpgsqlCommand();
            result.CommandText = $@"SELECT {_metaTableName}.{PostgreConsts.Local}, 
                                           {_metaTableName}.{PostgreConsts.IsDeleted},
                                           {_metaTableName}.{PostgreConsts.DeleteTime},
                                           {_metaTableName}.{_keyName} AS MetaId,
                                           UserScriptResult.{_userKeyName} AS UserId,
                                           {_metaTableName}.{PostgreConsts.Hash}
                                    FROM ( {userRead.CommandText} ) AS UserScriptResult
                                    INNER JOIN {_metaTableName} ON UserScriptResult.{_userKeyName} = {_metaTableName}.{_keyName}
                                    WHERE {_metaTableName}.{_keyName} = @{_keyName}";

            return SetKeytoCommand(result, key);
        }


        public Tuple<MetaData, bool> ReadMetaDataFromReader(DbReader<NpgsqlDataReader> reader, bool readuserId = true)
        {
            object local = reader.GetValue(PostgreConsts.Local);
            object isDeleted = reader.GetValue(PostgreConsts.IsDeleted);
            object deleteTime = reader.GetValue(PostgreConsts.DeleteTime);
            object hash = reader.GetValue(PostgreConsts.Hash);

            MetaData meta = null;

            if (!(local is DBNull || isDeleted is DBNull))
            {
                bool l = GetLocalBack((int)local);
                bool i = GetLocalBack((int)isDeleted);
                DateTime? t = deleteTime is DBNull ? (DateTime?)null : (DateTime)deleteTime;

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

            if (!(local is DBNull || isDeleted is DBNull))
            {
                bool l = GetLocalBack((int)local);
                bool i = GetLocalBack((int)isDeleted);
                DateTime? t = deleteTime is DBNull ? (DateTime?)null : (DateTime)deleteTime;

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
            var result = new NpgsqlCommand();
            result.CommandText = $@"SELECT * FROM ( {userRead.CommandText} ) AS UserScriptResult
                                             INNER JOIN {_metaTableName} ON UserScriptResult.{_userKeyName} = {_metaTableName}.{_keyName}
                                             WHERE {_metaTableName}.{_keyName} = @{_keyName} AND {_metaTableName}.{PostgreConsts.IsDeleted} = {IsDeleted(isDelete)}";


            return SetKeytoCommand(result, key);
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