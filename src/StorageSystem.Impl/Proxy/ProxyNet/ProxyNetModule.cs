using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.Proxy.Interfaces;

namespace Qoollo.Impl.Proxy.ProxyNet
{
    internal class ProxyNetModule : NetModule, IProxyNetModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private IProxyDistributorModule _distributor;

        public ProxyNetModule(StandardKernel kernel,
            ConnectionTimeoutConfiguration connectionTimeout) : base(kernel, connectionTimeout)
        {
        }

        public override void Start()
        {
            _distributor = Kernel.Get<IProxyDistributorModule>();
        }

        public void PingDistributors(List<ServerId> servers, Action<ServerId> serverAvailable)
        {
            PingServers(servers, serverAvailable, id => FindServer(id) as SingleConnectionToDistributor,
                ConnectToDistributor);
        }        

        public bool ConnectToDistributor(ServerId server)
        {
            return ConnectToServer(server,
                (id, time) => new SingleConnectionToDistributor(Kernel, id, time));
        }

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
                _logger.DebugFormat("ProxyNetModule: process fail result  server: {0}, result: {1}", server, ret);
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
                _logger.DebugFormat("ProxyNetModule: process fail result  server: {0}, result: {1}", server, ret);
                RemoveConnection(server);
            }

            return ret;
        }

        #endregion        
    }
}
