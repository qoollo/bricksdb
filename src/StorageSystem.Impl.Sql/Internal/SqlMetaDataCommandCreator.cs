using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.Writer.Db.Commands;

namespace Qoollo.Impl.Sql.Internal
{
    internal class SqlMetaDataCommandCreator<TKey, TValue>:IMetaDataCommandCreator<SqlCommand, SqlDataReader>
    {
        private string _keyName;
        private string _userKeyName;
        private string _metaTableName = "MetaTable";

        private SqlDbType _keyType;
        private readonly SqlScriptParser _scriptParser;

        private readonly UserCommandsHandler<SqlCommand, SqlDbType, SqlConnection, TKey, TValue, SqlDataReader> _handler;

        public SqlMetaDataCommandCreator(
            IUserCommandCreator<SqlCommand, SqlConnection, TKey, TValue, SqlDataReader> userCommandCreator)
        {
            _scriptParser = new SqlScriptParser();
            _handler =
                new UserCommandsHandler<SqlCommand, SqlDbType, SqlConnection, TKey, TValue, SqlDataReader>(
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

        #region MetaData

        public SqlCommand InitMetaDataDb(string idInit)
        {
            return new SqlCommand(string.Format("create table {1} ({0} not null primary key, " +
                                                "{2} int not null, " +
                                                "{3} int not null, " +
                                                "{4} datetime); " +
                                                "CREATE NONCLUSTERED INDEX [NonClusteredIndex-20140609-052749] ON [dbo].{1} " +
                                                "([{3}] ASC, " +
                                                "[{2}] ASC " +
                                                " )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF," +
                                                " ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] "
                , idInit, _metaTableName, SqlConsts.Local, SqlConsts.IsDeleted, SqlConsts.DeleteTime));
        }

        public SqlCommand CreateMetaData(bool remote)
        {
            return new SqlCommand(string.Format("insert into {0} ({1}, {3}, {4}, {5})  " +
                                                "values (@{1}, {2}, 1,'');", _metaTableName, _keyName, GetLocal(remote),
                SqlConsts.Local, SqlConsts.IsDeleted, SqlConsts.DeleteTime));
        }

        public SqlCommand DeleteMetaData()
        {
            return new SqlCommand(string.Format("delete from {0} " +
                                                "where {1} = @{1};", _metaTableName, _keyName));
        }

        public SqlCommand UpdateMetaData(bool local)
        {
            return new SqlCommand(string.Format("update {0} " +
                                                "set {3} = {1} " +
                                                "where {2} = @{2};", _metaTableName, GetLocal(local), _keyName,
                                                SqlConsts.Local));
        }

        public SqlCommand SetDataDeleted()
        {
            var command = new SqlCommand(string.Format("update {0} " +
                                                       "set {2} = 0," +
                                                       "{3} = @time " +
                                                       "where {1} = @{1};", _metaTableName, _keyName,
                SqlConsts.IsDeleted, SqlConsts.DeleteTime));
            command.Parameters.Add("@time", SqlDbType.DateTime);
            command.Parameters["@time"].Value = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            return command;
        }

        public SqlCommand SetDataNotDeleted()
        {
            return new SqlCommand(string.Format("update {0} " +
                                                "set {2} = 1 " +
                                                "where {1} = @{1};", _metaTableName, _keyName, SqlConsts.IsDeleted));
        }

        public SqlCommand ReadMetaData(SqlCommand userRead)
        {
            string script =
                string.Format(
                    "select {0}.{3}, {0}.{4}, {0}.{5}, {0}.{1} as 'MetaId', HelpTable.{6} as 'UserId' " +
                    " from ( {2} ) as HelpTable right join {0} on HelpTable.{6} = {0}.{1} " +
                    " where {0}.{1} = @{1}",
                    _metaTableName, _keyName, userRead.CommandText, SqlConsts.Local, SqlConsts.IsDeleted,
                    SqlConsts.DeleteTime, _userKeyName);

            return new SqlCommand(script);
        }

        public Tuple<MetaData, bool> ReadMetaDataFromReader(DbReader<SqlDataReader> reader, bool readuserId = true)
        {
            object local = reader.GetValue(SqlConsts.Local);
            object isDeleted = reader.GetValue(SqlConsts.IsDeleted);
            object deleteTime = reader.GetValue(SqlConsts.DeleteTime);

            MetaData meta = null;

            if (!(local is DBNull || isDeleted is DBNull || deleteTime is DBNull))
            {
                bool l = GetLocalBack((int) local);
                bool i = GetLocalBack((int) isDeleted);
                var t = (DateTime) deleteTime;

                meta = new MetaData(l, t, i);
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
            object local = data.Fields.Find(x => x.Item2.ToLower() == SqlConsts.Local.ToLower()).Item1;
            object isDeleted = data.Fields.Find(x => x.Item2.ToLower() == SqlConsts.IsDeleted.ToLower()).Item1;
            object deleteTime = data.Fields.Find(x => x.Item2.ToLower() == SqlConsts.DeleteTime.ToLower()).Item1;
            object id = data.Fields.Find(x => x.Item2.ToLower() == _keyName.ToLower()).Item1;

            MetaData meta = null;

            if (!(local is DBNull || isDeleted is DBNull || deleteTime is DBNull))
            {
                bool l = GetLocalBack((int) local);
                bool i = GetLocalBack((int) isDeleted);
                var t = (DateTime) deleteTime;

                meta = new MetaData(l, t, i)
                {
                    Id = id
                };
            }

            return meta;
        }

        #endregion

        public string ReadWithDeleteAndLocal(bool isDelete, bool local)
        {
            if (local)
                return string.Format("select * from  {0} " +
                                     " where {0}.{3} = {1}" +
                                     " order by {2}", _metaTableName, IsDeleted(isDelete), _keyName, SqlConsts.IsDeleted);

            return string.Format("select * from  {0} " +
                                 " where {0}.{4} = {1} and {0}.{5} = {2}" +
                                 " order by {3}",
                _metaTableName, GetLocal(false), IsDeleted(isDelete), _keyName,
                SqlConsts.Local, SqlConsts.IsDeleted);
        }

        public SqlCommand ReadWithDelete(SqlCommand userRead, bool isDelete)
        {
            string script = string.Format("select * from ( {0} ) as MetaHelpTable " +
                                          " inner join {1} on MetaHelpTable.{5} = {1}.{2}" +
                                          " where {1}.{2} = @{2} and {1}.{4} = {3}", userRead.CommandText,
                _metaTableName, _keyName, IsDeleted(isDelete), SqlConsts.IsDeleted, _userKeyName);

            return new SqlCommand(script);
        }

        public SqlCommand ReadWithDeleteAndLocal(SqlCommand userRead, bool isDelete, bool local)
        {
            if (local)
                return new SqlCommand(string.Format("select * from ( {0} ) as MetaHelpTable " +
                                                    " inner join {1} on MetaHelpTable.{5} = {1}.{2}" +
                                                    " where {1}.{4} = {3}" +
                                                    " order by {2}", userRead.CommandText, _metaTableName,
                    _keyName, IsDeleted(isDelete), SqlConsts.IsDeleted, _userKeyName));

            string script = string.Format("select * from ( {0} ) as MetaHelpTable " +
                                          " inner join {1} on MetaHelpTable.{7} = {1}.{2}" +
                                          " where {1}.{5} = {4} and {1}.{6} = {3}" +
                                          " order by {2}", userRead.CommandText,
                _metaTableName, _keyName, IsDeleted(isDelete), GetLocal(false),
                 SqlConsts.Local, SqlConsts.IsDeleted, _userKeyName);

            return new SqlCommand(script);
        }

        public SqlCommand CreateSelectCommand(string script, FieldDescription idDescription,
            List<FieldDescription> userParameters)
        {
            string nquery = _scriptParser.CreateOrderScript(script, idDescription);

            var command = new SqlCommand(nquery);

            var name = idDescription.FieldName == _keyName ? _userKeyName : idDescription.FieldName;
            var dbtype = _handler.GetFieldsDescription().Find(x => x.Item1.ToLower() == name.ToLower());
            command.Parameters.Add("@" + idDescription.FieldName, dbtype.Item3);
            command.Parameters["@" + idDescription.FieldName].Value = idDescription.Value;

            foreach (var parameter in userParameters)
            {
                if (parameter.UserType >= 0 && parameter.UserType <= 34)
                {
                    command.Parameters.Add("@" + parameter.FieldName, (SqlDbType) parameter.UserType);
                    command.Parameters["@" + parameter.FieldName].Value = parameter.Value;
                }
            }

            return command;
        }

        public SqlCommand CreateSelectCommand(SqlCommand script, FieldDescription idDescription,
            List<FieldDescription> userParameters)
        {
            return CreateSelectCommand(script.CommandText, idDescription, userParameters);
        }

        public Dictionary<string, Type> GetFieldsDescription()
        {
            return new Dictionary<string, Type>
            {
                {SqlConsts.Local.ToLower(), typeof (int)},
                {SqlConsts.IsDeleted.ToLower(), typeof (int)},
                {SqlConsts.DeleteTime.ToLower(), typeof (string)}
            };
        }

        public SqlCommand SetKeytoCommand(SqlCommand command, object key)
        {
            command.Parameters.Add("@" + _keyName, _keyType);
            command.Parameters["@" + _keyName].Value = key;

            return command;
        }

        public List<Tuple<object, string>> SelectProcess(DbReader<SqlDataReader> reader)
        {
            var fields = new List<Tuple<object, string>>();

            for (int i = 0; i < reader.CountFields(); i++)
            {
                var name = reader.Reader.GetName(i).Split('.').ToList().Last();
                var v = reader.GetValue(i);
                if (v is DBNull)
                    v = null;
                fields.Add(new Tuple<object, string>(v, name));
            }

            return fields;
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
