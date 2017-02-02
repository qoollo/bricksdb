using Qoollo.Impl.Modules.Db.Impl;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Postgre
{
    public class PostgreConnectionParams: DbConnectionParams
    {
        public string ConnectionString { get; private set; }
        public int CountDbConnections { get; private set; }
        public int DbTrimPeriod { get; private set; }

        public PostgreConnectionParams(string connectionString, int countDbConnections, int dbTrimPeriod)
        {
            Contract.Requires(connectionString != "");

            DbTrimPeriod = dbTrimPeriod;
            ConnectionString = connectionString;
            CountDbConnections = countDbConnections;
        }
    }
}
