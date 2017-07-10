using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Collector.Interfaces;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Collector.CollectorNet
{
    internal class CollectorNetModule : NetModule, ICollectorNetModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private IDistributorModule _distributor;

        public CollectorNetModule(StandardKernel kernel, ConnectionConfiguration connectionConfiguration,
            ConnectionTimeoutConfiguration connectionTimeout)
            : base(kernel, connectionConfiguration, connectionTimeout)
        {
        }

        public override void Start()
        {
            _distributor = Kernel.Get<IDistributorModule>();
        }

        #region Connect to Writer

        public bool ConnectToWriter(ServerId server)
        {
            return ConnectToServer(server,
                (id, configuration, time) => new SingleConnectionToWriter(Kernel, id, configuration, time));
        }

        public void PingWriter(List<ServerId> servers, Action<ServerId> serverAvailable)
        {
            PingServers(servers, serverAvailable, id => FindServer(id) as SingleConnectionToWriter,
                ConnectToWriter);
        }

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(ServerId server, SelectDescription description)
        {
            var connection = FindServer(server) as SingleConnectionToWriter;
            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToWriter;
            }

            if (connection == null)
            {
                _logger.DebugFormat("CollectorNetModule: process server not found  server = {0}", server);
                _distributor.ServerUnavailable(server);
                return new Tuple<RemoteResult, SelectSearchResult>(new ServerNotFoundResult(), null);
            }

            var ret = connection.SelectQuery(description);
            if (ret.Item1 is FailNetResult)
            {
                _logger.DebugFormat("CollectorNetModule: process fail result  server = {0}, result = {1}", server, ret.Item1);
                _distributor.ServerUnavailable(server);
                RemoveConnection(server);
            }

            return ret;
        }

        #endregion

        #region Connect to Distributor

        public bool ConnectToDistributor(ServerId server)
        {
            return ConnectToServer(server,
                (id, configuration, time) => new SingleConnectionToDistributor(Kernel, id, configuration, time));
        }

        public void PingDistributors(List<ServerId> servers)
        {
            PingServers(servers, id => { }, id => FindServer(id) as SingleConnectionToDistributor,
                ConnectToDistributor);
        }

        public RemoteResult SendSyncToDistributor(ServerId server, NetCommand command)
        {
            var connection = FindServer(server) as SingleConnectionToDistributor;
            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                _logger.DebugFormat("CollectorNetModule: process server not found  server = {0}", server);
                _distributor.ServerUnavailable(server);
                return new ServerNotAvailable(server);
            }

            return connection.SendSync(command);
        }

        #endregion

        public new List<ServerId> GetServersByType(Type type)
        {
            return base.GetServersByType(type);
        }
    }

}
