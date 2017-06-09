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
        private readonly PostgreScriptParser _scriptParser;

        private readonly UserCommandsHandler<NpgsqlCommand, NpgsqlDbType, NpgsqlConnection, TKey, TValue, NpgsqlDataReader> _handler;

        public PostgreMetaDataCommandCreator(
            IUserCommandCreator<NpgsqlCommand, NpgsqlConnection, TKey, TValue, NpgsqlDataReader> userCommandCreator)
        {
            _scriptParser = new PostgreScriptParser();
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
            _keyName = PostgreHelper.NormalizeName("Meta_" + keyName.UnQuote());
            _userKeyName = keyName;
            var descr = _handler.GetFieldsDescription();
            _keyType = descr.Find(x => x.Item1 == keyName).Item3;
        }

        public void SetTableName(List<string> tableName)
        {
            _metaTableName = _metaTableName + "_" + string.Join("_", tableName.Select(o => o.UnQuote()));
        }

        public NpgsqlCommand SetKeytoCommand(NpgsqlCommand command, object key)
        {
            command.Parameters.Add("@" + _keyName, _keyType);
            command.Parameters["@" + _keyName].Value = key;

            return command;
        }

        public NpgsqlCommand InitMetaDataDb(string idInit)
        {
            idInit = idInit.TrimStart();
            int indexOfSpace = idInit.IndexOf(' ');
            if (indexOfSpace > 0)
                idInit = _keyName + idInit.Substring(indexOfSpace);

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
                DateTime? t = (deleteTime == null || (deleteTime is DBNull)) ? (DateTime?)null : (DateTime)deleteTime;

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

        public MetaData ReadMetaFromSearchData(SearchData data)
        {
            object local = data.Fields.Find(x => PostgreHelper.AreNamesEqual(x.Item2, true, PostgreConsts.Local, true)).Item1;
            object isDeleted = data.Fields.Find(x => PostgreHelper.AreNamesEqual(x.Item2, true, PostgreConsts.IsDeleted, true)).Item1;
            object deleteTime = data.Fields.Find(x => PostgreHelper.AreNamesEqual(x.Item2, true, PostgreConsts.DeleteTime, true)).Item1;
            object hash = data.Fields.Find(x => PostgreHelper.AreNamesEqual(x.Item2, true, PostgreConsts.Hash, true)).Item1;
            object id = data.Fields.Find(x => PostgreHelper.AreNamesEqual(x.Item2, true, _keyName, true)).Item1;

            MetaData meta = null;

            if (!(local is DBNull || isDeleted is DBNull))
            {
                bool l = GetLocalBack((int)local);
                bool i = GetLocalBack((int)isDeleted);
                DateTime? t = (deleteTime == null || (deleteTime is DBNull)) ? (DateTime?)null : (DateTime)deleteTime;

                meta = new MetaData(l, t, i, (string)hash)
                {
                    Id = id
                };
            }

            return meta;
        }

        public string ReadWithDeleteAndLocal(bool isDelete, bool local)
        {
            if (local)
            {
                return $@"SELECT * FROM {_metaTableName}
                          WHERE {_metaTableName}.{PostgreConsts.IsDeleted} = {IsDeleted(isDelete)}
                          ORDER BY {_keyName}";
            }
            else
            {
                return $@"SELECT * FROM {_metaTableName}
                          WHERE {_metaTableName}.{PostgreConsts.Local} = {GetLocal(false)} AND 
                                {_metaTableName}.{PostgreConsts.IsDeleted} = {IsDeleted(isDelete)}
                          ORDER BY {_keyName}";
            }
        }

        public NpgsqlCommand ReadWithDeleteAndLocalList(NpgsqlCommand userRead, bool isDelete, List<object> keys)
        {
            NpgsqlCommand result = new NpgsqlCommand();

            result.CommandText = $@"SELECT * FROM ( {userRead.CommandText} ) AS UserScriptResult
                                    INNER JOIN {_metaTableName} ON UserScriptResult.{_userKeyName} = {_metaTableName}.{_keyName}
                                    WHERE {_metaTableName}.{_keyName} = ANY(@list) AND
                                          {_metaTableName}.{PostgreConsts.IsDeleted} = {IsDeleted(isDelete)}";

            result.Parameters.Add("@list", NpgsqlDbType.Array | _keyType).Value = keys.ToArray();
            return result;
        }

        public NpgsqlCommand ReadWithDelete(NpgsqlCommand userRead, bool isDelete, object key)
        {
            var result = new NpgsqlCommand();
            result.CommandText = $@"SELECT * FROM ( {userRead.CommandText} ) AS UserScriptResult
                                    INNER JOIN {_metaTableName} ON UserScriptResult.{_userKeyName} = {_metaTableName}.{_keyName}
                                    WHERE {_metaTableName}.{_keyName} = @{_keyName} AND 
                                          {_metaTableName}.{PostgreConsts.IsDeleted} = {IsDeleted(isDelete)}";


            return SetKeytoCommand(result, key);
        }

        public NpgsqlCommand ReadWithDeleteAndLocal(NpgsqlCommand userRead, bool isDelete, bool local)
        {
            NpgsqlCommand result = new NpgsqlCommand();

            if (local)
            {
                result.CommandText = $@"SELECT * FROM ( {userRead.CommandText} ) AS UserScriptResult
                                        INNER JOIN {_metaTableName} ON UserScriptResult.{_userKeyName} = {_metaTableName}.{_keyName}
                                        WHERE {_metaTableName}.{PostgreConsts.IsDeleted} = {IsDeleted(isDelete)}
                                        ORDER BY {_keyName}";
            }
            else
            {
                result.CommandText = $@"SELECT * FROM ( {userRead.CommandText} ) AS UserScriptResult
                                        INNER JOIN {_metaTableName} ON UserScriptResult.{_userKeyName} = {_metaTableName}.{_keyName}
                                        WHERE {_metaTableName}.{PostgreConsts.IsDeleted} = {IsDeleted(isDelete)} AND
                                              {_metaTableName}.{PostgreConsts.Local} = {GetLocal(false)}
                                        ORDER BY {_keyName}";
            }

            return result;
        }


        private NpgsqlCommand CreateSelectCommandInner(string script, FieldDescription idDescription,
            List<FieldDescription> userParameters, bool useUserScript = false, List<FieldDescription> orderKeyParameters = null)
        {
            var command = new NpgsqlCommand(script);
            var name = idDescription.FieldName == _keyName ? PostgreHelper.NormalizeName(_userKeyName) : idDescription.FieldName;

            if (!useUserScript || !idDescription.IsFirstAsk)
            {
                var dbtype = _handler.GetFieldsDescription().Find(x => PostgreHelper.AreNamesEqual(x.Item1, false, name, true));
                command.Parameters.Add("@" + idDescription.FieldName, dbtype.Item3);
                command.Parameters["@" + idDescription.FieldName].Value = idDescription.Value;
            }

            foreach (var parameter in userParameters)
            {
                if (parameter.UserType >= 0 && parameter.UserType <= 39 &&
                    (idDescription.IsFirstAsk || !PostgreHelper.AreNamesEqual(parameter.FieldName, false, idDescription.FieldName, true)))
                {
                    command.Parameters.Add("@" + parameter.FieldName, (NpgsqlDbType)parameter.UserType);
                    command.Parameters["@" + parameter.FieldName].Value = parameter.Value;
                }
            }

            if (orderKeyParameters != null)
            {
                foreach (var parameter in orderKeyParameters)
                {
                    if (parameter.UserType >= 0 && parameter.UserType <= 39)
                    {
                        if (!PostgreHelper.AreNamesEqual(parameter.FieldName, true, idDescription.FieldName, true) ||
                            !command.Parameters.Contains("@" + parameter.FieldName))
                        {
                            NpgsqlParameter curPar = null;
                            if (parameter.UserType == 0) // We don't know the type
                                curPar = new NpgsqlParameter("@" + parameter.FieldName, parameter.Value);
                            else
                                curPar = new NpgsqlParameter("@" + parameter.FieldName, (NpgsqlDbType)parameter.UserType) { Value = parameter.Value };

                            command.Parameters.Add(curPar);
                        }
                    }
                }
            }

            return command;
        }

        public NpgsqlCommand CreateSelectCommand(string script, FieldDescription idDescription, List<FieldDescription> userParameters,
            List<FieldDescription> keysParameters)
        {
            if (keysParameters == null)
                return CreateSelectCommand(script, idDescription, userParameters); // Fallback to old impl

            string nquery = _scriptParser.CreateOrderScript(script, idDescription, keysParameters);
            return CreateSelectCommandInner(nquery, idDescription, userParameters, orderKeyParameters: keysParameters);
        }

        public NpgsqlCommand CreateSelectCommand(string script, FieldDescription idDescription,
            List<FieldDescription> userParameters)
        {
            string nquery = _scriptParser.CreateOrderScript(script, idDescription);
            return CreateSelectCommandInner(nquery, idDescription, userParameters);
        }

        public NpgsqlCommand CreateSelectCommand(SelectDescription description)
        {
            if (!description.UseUserScript)
                return CreateSelectCommand(description.Script, description.IdDescription,
                    description.UserParametrs, description.OrderKeyDescriptions);

            return CreateSelectCommandInner(description.Script, description.IdDescription, description.UserParametrs,
                true, description.OrderKeyDescriptions);
        }

        public NpgsqlCommand CreateSelectCommand(NpgsqlCommand script, FieldDescription idDescription, List<FieldDescription> userParameters)
        {
            return CreateSelectCommand(script.CommandText, idDescription, userParameters, new List<FieldDescription>());
        }

        public List<Tuple<object, string>> SelectProcess(DbReader<NpgsqlDataReader> reader)
        {
            var fields = new List<Tuple<object, string>>();

            for (int i = 0; i < reader.CountFields(); i++)
            {
                var name = reader.Reader.GetName(i);
                int lastPointIndex = name.LastIndexOf('.');
                if (lastPointIndex >= 0)
                    name = name.Substring(lastPointIndex + 1);

                var value = reader.GetValue(i);
                if (value is DBNull) value = null;

                fields.Add(new Tuple<object, string>(value, PostgreHelper.NormalizeName(name)));
            }

            return fields;
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
            var field = _handler.GetDbFieldsDescription().FirstOrDefault(x => PostgreHelper.AreNamesEqual(x.Item1, false, _userKeyName, false));

            var description = new FieldDescription(_keyName, field.Item2)
            {
                IsFirstAsk = true
            };
            return description;
        }
    }
}