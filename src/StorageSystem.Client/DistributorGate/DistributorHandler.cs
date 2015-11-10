using System.Linq;
using Qoollo.Client.Request;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;

namespace Qoollo.Client.DistributorGate
{
    internal class DistributorHandler:IDistributorApi
    {
        private readonly DistributorSystem _distributorSystem;

        public DistributorHandler(DistributorSystem distributorSystem)
        {
            _distributorSystem = distributorSystem;
        }

        public RequestDescription GetDistributors()
        {
            var list = _distributorSystem.Distributor.GetDistributors();
            var result = list.Aggregate(string.Empty, (current, serverId) => current + string.Format("{0}\n", serverId));
            return new RequestDescription(result);
        }

        public void UpdateModel()
        {
            _distributorSystem.Distributor.UpdateModel();
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            var result = _distributorSystem.Distributor.SayIAmHereRemoteResult(new ServerId(host,  port));
            return new RequestDescription(result);
        }

        public RequestDescription GetServersState()
        {
            return new RequestDescription(_distributorSystem.Distributor.GetServersState());
        }
    }
}
