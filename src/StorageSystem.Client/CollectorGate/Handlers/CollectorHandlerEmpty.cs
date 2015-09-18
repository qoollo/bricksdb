using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Impl.Collector.Parser;

namespace Qoollo.Client.CollectorGate.Handlers
{
    internal class CollectorHandlerEmpty : ICollectorApi
    {
        public StorageDbReader CreateReader(string query, bool isUseUserScript = false, FieldDescription field = null)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount, bool isUseUserScript = false, FieldDescription field = null)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, bool isUseUserScript = false, FieldDescription field = null)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, List<QueryParameter> parameters, bool isUseUserScript = false, FieldDescription field = null)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount, List<QueryParameter> parameters,
            bool isUseUserScript = false, FieldDescription field = null)
        {
            return null;
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, List<QueryParameter> parameters,
            bool isUseUserScript = false, FieldDescription field = null)
        {
            return null;
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return new RequestDescription();
        }
    }
}
