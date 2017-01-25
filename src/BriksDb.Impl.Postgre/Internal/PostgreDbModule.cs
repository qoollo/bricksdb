using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using Npgsql;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.Postgre.Internal
{
    internal class PostgreDbModule : DbImplModuleWithPool<NpgsqlConnection, PostgreConnectionParams, NpgsqlCommand, NpgsqlDataReader>
    {
        public PostgreDbModule(PostgreConnectionParams connectionParam, int maxCountElementInPool, int trimPeriod)
            : base(connectionParam, maxCountElementInPool, trimPeriod)
        {
        }

        public override RemoteResult ExecuteNonQuery(NpgsqlCommand command)
        {
            RemoteResult ret = null;
            using (var element = RentConnection())
            {
                command.Connection = element.Element;
                try
                {
                    command.ExecuteNonQuery();
                    command.Dispose();
                    ret = new SuccessResult();

                }
                catch (SqlException e)
                {
                    Logger.Logger.Instance.Error(e, "");
                    ret = new FailNetResult(e.Message);
                }
                catch (NpgsqlException e)
                {
                    Logger.Logger.Instance.Error(e, "");
                    ret = new FailNetResult(e.Message);
                }
                catch (IOException e)
                {
                    Logger.Logger.Instance.Error(e, "");
                    ret = new FailNetResult(e.Message);
                }
                catch (InvalidOperationException e)
                {
                    Logger.Logger.Instance.Error(e, "");
                    ret = new FailNetResult(e.Message);
                }
            }

            return ret;
        }

        public override DbReader<NpgsqlDataReader> CreateReader(NpgsqlCommand command)
        {
            return new PostgreReader(command, RentConnection());
        }

        public override RentedElementMonitor<NpgsqlConnection> RentConnectionInner()
        {
            return RentConnection();
        }


        protected override bool CreateElement(out NpgsqlConnection elem, PostgreConnectionParams connectionParam,
            int timeout,
            CancellationToken token)
        {
            elem = new NpgsqlConnection(connectionParam.ConnectionString);
            try
            {
                elem.Open();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
                return false;
            }
            return true;
        }

        protected override bool IsValidElement(NpgsqlConnection elem)
        {
            return elem != null && elem.State == ConnectionState.Open;
        }

        protected override void DestroyElement(NpgsqlConnection elem)
        {
            try
            {
                elem.Close();
                elem.Dispose();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
            }
        }
    }
}
