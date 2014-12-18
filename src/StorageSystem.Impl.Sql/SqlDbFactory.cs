using System.Data;
using System.Data.SqlClient;
using Qoollo.Client.StorageGate;
using Qoollo.Client.Support;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.DbController.Db;
using Qoollo.Impl.DbController.Db.Commands;
using Qoollo.Impl.Sql.Internal;

namespace Qoollo.Impl.Sql
{
    public class SqlDbFactory<TKey, TValue> : DbFactory
    {
        private readonly IUserCommandCreator<SqlCommand, SqlConnection, TKey, TValue, SqlDataReader> _userCommandCreator;
        private readonly IDataProvider<TKey, TValue> _dataProvider;
        private readonly SqlConnectionParams _connectionParams;
        private readonly bool _hashFromValue;

        public SqlDbFactory(IDataProvider<TKey, TValue> dataProvider,
            SqlUserCommandCreator< TKey, TValue> userCommandCreator,
            SqlConnectionParams connectionParams, bool hashFromValue)
        {
            _userCommandCreator = new SqlUserCommandCreatorInner<TKey, TValue>(userCommandCreator);
            _dataProvider = dataProvider;
            _connectionParams = connectionParams;
            _hashFromValue = hashFromValue;
        }

        public SqlDbFactory(SqlUserCommandCreator<TKey, TValue> userCommandCreator)
        {
            _userCommandCreator = new SqlUserCommandCreatorInner<TKey, TValue>(userCommandCreator);
        }

        public override DbModule Build()
        {
            return
                new DbLogicModule<SqlCommand, TKey, TValue, SqlConnection, SqlDataReader>(
                    new HashFakeImpl<TKey, TValue>(_dataProvider), _hashFromValue,_userCommandCreator,
                    new SqlMetaDataCommandCreator<TKey, TValue>(_userCommandCreator),
                    new SqlDbModule(_connectionParams, _connectionParams.CountDbConnections,
                        _connectionParams.DbTrimPeriod));
        }

        public override ScriptParser GetParser()
        {
            var parser = new SqlScriptParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<SqlCommand, SqlDbType, SqlConnection, TKey, TValue, SqlDataReader>(
                    _userCommandCreator, new SqlMetaDataCommandCreator<TKey, TValue>(_userCommandCreator)));

            return parser;
        }
    }
}
