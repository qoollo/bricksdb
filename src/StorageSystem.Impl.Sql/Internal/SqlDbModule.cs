using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.Sql.Internal
{
    internal class SqlDbModule : DbImplModuleWithPool<SqlConnection, SqlConnectionParams, SqlCommand, SqlDataReader>
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public SqlDbModule(SqlConnectionParams connectionParam, int maxCountElementInPool, int trimPeriod)
            : base(connectionParam, maxCountElementInPool, trimPeriod)
        {
        }

        public override RemoteResult ExecuteNonQuery(SqlCommand command)
        {
            RemoteResult ret = null;
            using (var element = RentConnection())
            {
                command.Connection = element.Element;
                try
                {
                    command.ExecuteNonQuery();
                    ret = new SuccessResult();
                }
                catch (SqlException e)
                {
                    _logger.Error(e, "");
                    ret = new FailNetResult(e.Message);
                }
                catch (IOException e)
                {
                    _logger.Error(e, "");
                    ret = new FailNetResult(e.Message);
                }
                catch (InvalidOperationException e)
                {
                    _logger.Error(e, "");
                    ret = new FailNetResult(e.Message);
                }
                finally
                {
                    command.Dispose();
                }
            }

            return ret;
        }

        public override DbReader<SqlDataReader> CreateReader(SqlCommand command)
        {
            return new SqlReader(command, RentConnection());
        }

        public override RentedElementMonitor<SqlConnection> RentConnectionInner()
        {
            return RentConnection();
        }

        protected override bool CreateElement(out SqlConnection elem, SqlConnectionParams connectionParam, int timeout, CancellationToken token)
        {
            elem = new SqlConnection(connectionParam.ConnectionString);
            try
            {
                elem.Open();
            }
            catch (Exception e)
            {
                _logger.Warn(e, "");
                return false;
            }
            return true;
        }

        protected override bool IsValidElement(SqlConnection elem)
        {
            return elem != null && elem.State == ConnectionState.Open;
        }

        protected override void DestroyElement(SqlConnection elem)
        {
            try
            {
                elem.Close();
                elem.Dispose();
            }
            catch (Exception e)
            {
                _logger.Warn(e, "");
            }
        }
    }
}
