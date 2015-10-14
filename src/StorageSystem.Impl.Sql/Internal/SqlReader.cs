﻿using System;
using System.Data.SqlClient;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.Sql.Internal
{
    internal class SqlReader:DbReader<SqlDataReader>
    {
        private SqlCommand _command;
        private readonly RentedElementMonitor<SqlConnection> _connection;
        private SqlDataReader _reader;

        public SqlReader(SqlCommand command, RentedElementMonitor<SqlConnection> connection)
        {
            _command = command;
            _connection = connection;
        }

        public override SqlDataReader Reader
        {
            get { return _reader; }
        }

        public override bool IsCanRead
        {
            get { return _reader.Read(); }
        }

        protected override int CountFieldsInner()
        {
            return _reader.FieldCount;
        }

        protected override void ReadNextInner()
        {            
        }

        protected override object GetValueInner(int index)
        {
            return _reader.GetValue(index);
        }

        protected override object GetValueInner(string index)
        {
            return _reader[index];
        }

        protected override bool IsValidRead()
        {
            return _reader != null && !_reader.IsClosed;
        }

        protected override void StartInner()
        {
            _command.Connection = _connection.Element;
            try
            {
                _reader = _command.ExecuteReader();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Error(e, "Command = " + _command.CommandText);
            }            

        }

        protected override void Dispose(bool isUserCall)
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader.Dispose();   
            }            
            _command.Dispose();
            _connection.Dispose();
        }
    }
}
