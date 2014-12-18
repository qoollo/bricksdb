using System.Collections.Generic;
using Qoollo.Client.Request;

namespace Qoollo.Client.CollectorGate.Handlers
{
    internal class CollectorHandlerEmpty : ICollectorApi
    {
        public StorageDbReader CreateReader(string query)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, List<QueryParameter> parameters)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount, List<QueryParameter> parameters)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, List<QueryParameter> parameters)
        {
            return null;
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return new RequestDescription();
        }
    }
}
