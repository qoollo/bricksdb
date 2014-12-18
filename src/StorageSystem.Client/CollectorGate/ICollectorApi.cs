using System.Collections.Generic;
using Qoollo.Client.Request;

namespace Qoollo.Client.CollectorGate
{
    public interface ICollectorApi
    {
        StorageDbReader CreateReader(string query);

        StorageDbReader CreateReader(string query, int limitCount);

        StorageDbReader CreateReader(string query, int limitCount, int userPage);

        StorageDbReader CreateReader(string query, List<QueryParameter> parameters);

        StorageDbReader CreateReader(string query, int limitCount, List<QueryParameter> parameters);

        StorageDbReader CreateReader(string query, int limitCount, int userPage, List<QueryParameter> parameters);

        RequestDescription SayIAmHere(string host, int port);
    }
}
