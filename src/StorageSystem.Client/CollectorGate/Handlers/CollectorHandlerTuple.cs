using System;
using System.Collections.Generic;
using Qoollo.Client.Request;

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

        public StorageDbReader CreateReader(string query)
        {
            return InnerFunc(handler => handler.CreateReader(query));
        }

        public StorageDbReader CreateReader(string query, int limitCount)
        {
            return InnerFunc(handler => handler.CreateReader(query, limitCount));
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage)
        {
            return InnerFunc(handler => handler.CreateReader(query, limitCount, userPage));
        }

        public StorageDbReader CreateReader(string query, List<QueryParameter> parameters)
        {
            return InnerFunc(handler => handler.CreateReader(query, parameters));
        }

        public StorageDbReader CreateReader(string query, int limitCount, List<QueryParameter> parameters)
        {
            return InnerFunc(handler => handler.CreateReader(query, limitCount, parameters));
        }

        public StorageDbReader CreateReader(string query, int limitCount, int userPage, List<QueryParameter> parameters)
        {
            return InnerFunc(handler => handler.CreateReader(query, limitCount, userPage, parameters));
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return InnerFunc(handler => handler.SayIAmHere(host, port));
        }
    }
}
