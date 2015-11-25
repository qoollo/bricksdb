using System;
using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.Exceptions;

namespace Qoollo.Client.DistributorGate
{
    internal class DistributorHandlerEmpty : IDistributorApi
    {
        public List<ServerAddress> GetDistributors()
        {
            throw new InitializationException("System disposed, or not started");
        }

        public RequestDescription UpdateModel()
        {
            return new RequestDescription();
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return new RequestDescription();
        }

        public string GetServersState()
        {
            return new RequestDescription().ErrorDescription;
        }

        public RequestDescription AutoRestoreSetMode(bool mode)
        {
            return new RequestDescription();
        }
    }
}
