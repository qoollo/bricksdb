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

        public StorageDbReader CreateReader(string query, bool isUseUserScript = false, FieldDescription field = null)
        {
            var reader = _collectorSystem.CreateReader(query, isUseUserScript, field);
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount, bool isUseUserScript = false, FieldDescription field = null)
        {
            var reader = _collectorSystem.CreateReader(query, limitCount, isUseUserScript, field);
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, bool isUseUserScript = false, FieldDescription field = null)
        {
            var reader = _collectorSystem.CreateReader(query, limitCount, userPage, isUseUserScript, field);
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, List<QueryParameter> parameters, bool isUseUserScript = false, FieldDescription field = null)
        {
            if (parameters == null)
                return null;

            var reader = _collectorSystem.CreateReader(query, ConverParameters(parameters), isUseUserScript, field);
            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount, List<QueryParameter> parameters,
            bool isUseUserScript = false, FieldDescription field = null)
        {
            if (parameters == null)
                return null;

            var reader = _collectorSystem.CreateReader(query, limitCount,
                ConverParameters(parameters), isUseUserScript, field);

            reader.Start();
            return new StorageDbReader(reader);
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, List<QueryParameter> parameters,
            bool isUseUserScript = false, FieldDescription field = null)
        {
            if (parameters == null)
                return null;

            var reader = _collectorSystem.CreateReader(query, limitCount, userPage,
                ConverParameters(parameters), isUseUserScript, field);
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
