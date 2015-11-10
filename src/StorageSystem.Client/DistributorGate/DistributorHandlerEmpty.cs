using System;
using Qoollo.Client.Request;

namespace Qoollo.Client.DistributorGate
{
    internal class DistributorHandlerEmpty : IDistributorApi
    {
        public RequestDescription GetDistributors()
        {
            return new RequestDescription();
        }

        public void UpdateModel()
        {
            throw new Exception(new RequestDescription().ErrorDescription);
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return new RequestDescription();
        }

        public RequestDescription GetServersState()
        {
            return new RequestDescription();
        }
    }
}
