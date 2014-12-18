using Qoollo.Client.Request;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;

namespace Qoollo.Client.DistributorGate
{
    internal class DistributorHandler:IDistributorApi
    {
        private DistributorSystem _distributorSystem;

        public DistributorHandler(DistributorSystem distributorSystem)
        {
            _distributorSystem = distributorSystem;
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            var result = _distributorSystem.Distributor.SayIAmHereRemoteResult(new ServerId(host,  port));
            return new RequestDescription(result);
        }
    }
}
