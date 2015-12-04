using System.Collections.Generic;
using System.Linq;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
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

        public List<ServerAddress> GetDistributors()
        {
            var list = _distributorSystem.Distributor.GetDistributors();            
            return list.Select(x => new ServerAddress(x.RemoteHost, x.Port)).ToList();
        }

        public RequestDescription UpdateModel()
        {
            return new RequestDescription(_distributorSystem.Distributor.UpdateModel());
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            var result = _distributorSystem.Distributor.SayIAmHereRemoteResult(new ServerId(host,  port));
            return new RequestDescription(result);
        }

        public string GetServersState()
        {
            return _distributorSystem.Distributor.GetServersState();
        }

        public RequestDescription AutoRestoreSetMode(bool mode)
        {
            return new RequestDescription(_distributorSystem.Distributor.AutoRestoreSetMode(mode));
        }

        public RequestDescription Restore(ServerAddress restoreServer, ServerAddress remoteRestoreServer,
            RestoreMode mode)
        {
            return
                new RequestDescription(_distributorSystem.Distributor.Restore(restoreServer.ConvertServer(),
                    remoteRestoreServer == null ? null : remoteRestoreServer.ConvertServer(),
                    RestoreModeConverter.Convert(mode)));
        }
    }
}
