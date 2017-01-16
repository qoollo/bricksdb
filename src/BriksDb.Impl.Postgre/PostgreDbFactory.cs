﻿using System;
using Npgsql;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Postgre.Internal;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Db.Commands;

namespace Qoollo.Impl.Postgre
{
    public class PostgreDbFactory<TKey, TValue> : DbFactory
    {
        private readonly IUserCommandCreator<NpgsqlCommand, NpgsqlConnection, TKey, TValue, NpgsqlDataReader> _userCommandCreator;
        private readonly IDataProvider<TKey, TValue> _dataProvider;
        private readonly PostgreConnectionParams _connectionParams;

        public PostgreDbFactory(IDataProvider<TKey, TValue> dataProvider,
            PostgreUserCommandCreator<TKey, TValue> userCommandCreator,
            PostgreConnectionParams connectionParams, bool hashFromValue)
        {
            _userCommandCreator = new PostgreUserCommandCreatorInner<TKey, TValue>(userCommandCreator);
            _dataProvider = dataProvider;
            _connectionParams = connectionParams;
        }

        public PostgreDbFactory(PostgreUserCommandCreator<TKey, TValue> userCommandCreator)
        {
            _userCommandCreator = new PostgreUserCommandCreatorInner<TKey, TValue>(userCommandCreator);
        }

        public override DbModule Build()
        {
            return
                new DbLogicModule<NpgsqlCommand, TKey, TValue, NpgsqlConnection, NpgsqlDataReader>(
                    new HashFakeImpl<TKey, TValue>(_dataProvider), _userCommandCreator,
                    new PostgreMetaDataCommandCreator<TKey, TValue>(_userCommandCreator),
                    new PostgreDbModule(_connectionParams, _connectionParams.CountDbConnections,
                        _connectionParams.DbTrimPeriod));
        }

        public override ScriptParser GetParser()
        {
            //var parser = new SqlScriptParser();
            //parser.SetCommandsHandler(
            //    new UserCommandsHandler<SqlCommand, SqlDbType, SqlConnection, TKey, TValue, SqlDataReader>(
            //        _userCommandCreator, new SqlMetaDataCommandCreator<TKey, TValue>(_userCommandCreator)));

            //return parser;
            //TODO
            throw new NotImplementedException();
        }
    }
}