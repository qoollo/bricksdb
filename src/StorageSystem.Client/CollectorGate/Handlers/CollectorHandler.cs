using System.Collections.Generic;
using System.Linq;
using Qoollo.Client.Request;
using Qoollo.Impl.Collector;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;

namespace Qoollo.Client.CollectorGate.Handlers
{
    internal class CollectorHandler : ICollectorApi
    {
        private readonly SearchTaskModule _collectorSystem;
        private readonly CollectorSystem _system;

        public CollectorHandler(SearchTaskModule collectorSystem, CollectorSystem system)
        {
            _collectorSystem = collectorSystem;
            _system = system;
        }

        public StorageDbReader CreateReader(string query)
        {
            var reader = _collectorSystem.CreateReader(query);
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount)
        {
            var reader = _collectorSystem.CreateReader(query, limitCount);
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage)
        {
            var reader = _collectorSystem.CreateReader(query, limitCount, userPage);
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, List<QueryParameter> parameters)
        {
            if (parameters == null)
                return null;

            var reader = _collectorSystem.CreateReader(query, ConverParameters(parameters));
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount, List<QueryParameter> parameters)
        {
            if (parameters == null)
                return null;

            var reader = _collectorSystem.CreateReader(query, limitCount,
                ConverParameters(parameters));
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, List<QueryParameter> parameters)
        {
            if (parameters == null)
                return null;

            var reader = _collectorSystem.CreateReader(query, limitCount, userPage,
                ConverParameters(parameters));
            reader.Start();
            return new StorageDbReader(reader);
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            var result = _system.Distributor.SayIAmHere(new ServerId(host, port));
            return new RequestDescription(result);
        }

        private List<FieldDescription> ConverParameters(List<QueryParameter> parameters)
        {
            return parameters.Select(x => new FieldDescription(x.Name, (int)x.Type, x.Value)).ToList();
        }
    }
}
