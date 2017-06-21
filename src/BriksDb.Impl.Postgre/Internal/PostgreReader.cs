using System;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using Npgsql;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.Postgre.Internal
{
    class PostgreReader : DbReader<NpgsqlDataReader>
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private NpgsqlCommand _command;
        private readonly RentedElementMonitor<NpgsqlConnection> _connection;
        private NpgsqlDataReader _reader;

        public PostgreReader(NpgsqlCommand command, RentedElementMonitor<NpgsqlConnection> connection)
        {
            _command = command;
            _connection = connection;
        }

        public override NpgsqlDataReader Reader
        {
            get { return _reader; }
        }

        public override bool IsCanRead
        {
            get
            {
                try
                {
                    return _reader.Read();
                }
                catch (SqlException e)
                {
                    _logger.Error(e, "");
                }
                catch (NpgsqlException e)
                {
                    _logger.Error(e, "");
                }
                return false;
            }
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
                _logger.Error(e, "Command = " + _command.CommandText);
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