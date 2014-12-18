using System.Diagnostics.Contracts;
using Qoollo.Impl.Modules.Db.Impl;

namespace Qoollo.Impl.Sql
{
    public class SqlConnectionParams:DbConnectionParams
    {
        public string ConnectionString { get; private set; }
        public int CountDbConnections { get; private set; }
        public int DbTrimPeriod { get; private set; }

        public SqlConnectionParams(string connectionString, int countDbConnections, int dbTrimPeriod)
        {
            DbTrimPeriod = dbTrimPeriod;
            Contract.Requires(connectionString!="");
            ConnectionString = connectionString;
            this.CountDbConnections = countDbConnections;
        }
    }
}
