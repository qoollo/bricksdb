using System;
using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Impl.Collector.Parser;

namespace Qoollo.Client.CollectorGate.Handlers
{
    internal class CollectorHandlerTuple : ICollectorApi
    {
        private readonly Func<bool> _isEmpty;

        public CollectorHandlerTuple(CollectorHandlerEmpty collectorHandlerEmpty, CollectorHandler collectorHandler,
            Func<bool> isEmpty)
        {
            CollectorHandler = collectorHandler;
            _isEmpty = isEmpty;
            CollectorHandlerEmpty = collectorHandlerEmpty;
        }

        public CollectorHandlerEmpty CollectorHandlerEmpty { get; private set; }
        public CollectorHandler CollectorHandler { get; private set; }

        private TRet InnerFunc<TRet>(Func<ICollectorApi, TRet> func)
        {
            if (_isEmpty())
                return func(CollectorHandlerEmpty);
            return func(CollectorHandler);
        }

        public StorageDbReader CreateReader(string query, bool isUseUserScript = false, FieldDescription field = null)
        {
            return InnerFunc(handler => handler.CreateReader(query, isUseUserScript, field));
        }

        public StorageDbReader CreateReader(string query, int limitCount, bool isUseUserScript = false,
            FieldDescription field = null)
        {
            return InnerFunc(handler => handler.CreateReader(query, limitCount, isUseUserScript, field));
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, bool isUseUserScript = false,
            FieldDescription field = null)
        {
            return InnerFunc(handler => handler.CreateReader(query, limitCount, userPage, isUseUserScript, field));
        }

        public StorageDbReader CreateReader(string query, List<QueryParameter> parameters, bool isUseUserScript = false,
            FieldDescription field = null)
        {
            return InnerFunc(handler => handler.CreateReader(query, parameters, isUseUserScript, field));
        }

        public StorageDbReader CreateReader(string query, int limitCount, List<QueryParameter> parameters,
            bool isUseUserScript = false, FieldDescription field = null)
        {
            return InnerFunc(handler => handler.CreateReader(query, limitCount, parameters, isUseUserScript, field));
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, List<QueryParameter> parameters,
            bool isUseUserScript = false, FieldDescription field = null)
        {
            return
                InnerFunc(
                    handler => handler.CreateReader(query, limitCount, userPage, parameters, isUseUserScript, field));
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return InnerFunc(handler => handler.SayIAmHere(host, port));
        }
    }
}
