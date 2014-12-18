using Qoollo.Client.Request;

namespace Qoollo.Client.DistributorGate
{
    internal class DistributorHandlerEmpty : IDistributorApi
    {
        public RequestDescription SayIAmHere(string host, int port)
        {
            return new RequestDescription();
        }
    }
}
