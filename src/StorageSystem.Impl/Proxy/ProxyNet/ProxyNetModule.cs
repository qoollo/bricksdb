using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;

namespace Qoollo.Impl.Proxy.ProxyNet
{
    internal class ProxyNetModule:NetModule, IProxyNetModule
    {
        private ProxyDistributorModule _distributor;

        public ProxyNetModule(ConnectionConfiguration connectionConfiguration,
            ConnectionTimeoutConfiguration connectionTimeout) : base(connectionConfiguration, connectionTimeout)
        {
        }

        public void PingDistributors(List<ServerId> servers, Action<ServerId> serverAvailable)
        {
            PingServers(servers, serverAvailable, id => FindServer(id) as SingleConnectionToDistributor,
                ConnectToDistributor);
        }        

        #region Support

        public void SetDistributor(ProxyDistributorModule distributor)
        {
            Contract.Requires(distributor!=null);
            _distributor = distributor;
        }

        public bool ConnectToDistributor(ServerId server)
        {
            return ConnectToServer(server,
                (id, configuration, time) => new SingleConnectionToDistributor(id, configuration, time));
        }

        #endregion

        #region Interface

        public RemoteResult Process(ServerId server, InnerData ev)
        {
            var connection = FindServer(server) as SingleConnectionToDistributor;
            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                _distributor.ServerNotAvailable(server);
                return new ServerNotFoundResult();
            }

            var ret = connection.ProcessData(ev);
            if (ret is FailNetResult)
            {
                _distributor.ServerNotAvailable(server);
                RemoveConnection(server);
            }

            return ret;
        }

        public RemoteResult GetTransaction(ServerId server, UserTransaction transaction, out UserTransaction result)
        {
            var connection = FindServer(server) as SingleConnectionToDistributor;
            result = null;

            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                _distributor.ServerNotAvailable(server);
                return new ServerNotFoundResult();
            }

            result = connection.GetTransaction(transaction);
            if (result == null)
            {
                RemoveConnection(server);
                return new ConnectionErrorResult();
            }

            return new SuccessResult();
        }

        public RemoteResult SendDistributor(ServerId server, NetCommand command)
        {
            var connection = FindServer(server) as SingleConnectionToDistributor;

            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                _distributor.ServerNotAvailable(server);
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);

            if (ret is FailNetResult)
            {
                RemoveConnection(server);
            }

            return ret;
        }

        #endregion        
    }
}
