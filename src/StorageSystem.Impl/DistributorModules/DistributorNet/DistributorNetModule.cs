using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class DistributorNetModule:NetModule, INetModule
    {
        private DistributorModule _distributor;

        public DistributorNetModule(ConnectionConfiguration connectionConfiguration,
            ConnectionTimeoutConfiguration connectionTimeout) : base(connectionConfiguration, connectionTimeout)
        {
        }

        public void SetDistributor(DistributorModule distributor)
        {
            Contract.Requires(distributor != null);
            _distributor = distributor;
        }

        #region Connect to distributor

        public void PingDistributors(List<ServerId> servers)
        {
            PingServers(servers, id => { }, id => FindServer(id) as SingleConnectionToDistributor,
                ConnectToDistributor);
        }

        public virtual bool ConnectToDistributor(ServerId server)
        {
            return ConnectToServer(server,
                (serverId, configuration, time) => new SingleConnectionToDistributor(serverId, configuration, time));
        }

        public RemoteResult SendToDistributor(ServerId server, NetCommand command)
        {
            Logger.Logger.Instance.TraceFormat("SendSync command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToDistributor;
            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);
            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("DistributorNetModule: process fail result  server = {0}", server);

                RemoveConnection(server);
            }

            return ret;
        }

        #endregion
        
        #region Connect to proxy

        public void PingProxy(List<ServerId> servers)
        {
            PingServers(servers, id => { }, id => FindServer(id) as SingleConnectionToProxy,
                ConnectToProxy);
        }   

        public virtual bool ConnectToProxy(ServerId server)
        {
            return ConnectToServer(server, CreateConnectionToProxy);
        }

        protected virtual ISingleConnection CreateConnectionToProxy(ServerId server,
            ConnectionConfiguration configuration, ConnectionTimeoutConfiguration time)
        {
            return new SingleConnectionToProxy(server, configuration, time);
        }

        public RemoteResult SendToProxy(ServerId server, NetCommand command)
        {
            Logger.Logger.Instance.TraceFormat("SendSync to proxy command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToProxy;

            if (connection == null)
            {
                ConnectToProxy(server);
                connection = FindServer(server) as SingleConnectionToProxy;
            }

            if (connection == null)
            {
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);
            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("DistributorNetModule: process fail result  server = {0}", server);
                RemoveConnection(server);
            }

            return ret;
        }

        public RemoteResult ASendToProxy(ServerId server, NetCommand command)
        {
            Logger.Logger.Instance.TraceFormat("ASendSync to proxy command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToProxy;

            if (connection == null)
            {
                ConnectToProxy(server);
                connection = FindServer(server) as SingleConnectionToProxy;
            }

            if (connection == null)
            {
                return new ServerNotFoundResult();
            }

            var ret = connection.SendASyncWithResult(command);
            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("DistributorNetModule: process fail result  server = {0}", server);
                RemoveConnection(server);
            }

            return ret;
        }

        #endregion

        #region Connect to controller

        public void PingDbControllers(List<ServerId> servers, Action<ServerId> serverAvailable)
        {
            PingServers(servers, serverAvailable, id => FindServer(id) as SingleConnectionToDbController,
                ConnectToDbController);
        }

        public bool ConnectToDbController(ServerId server)
        {
            return ConnectToServer(server, CreateConnectionToDbController);
        }

        protected virtual ISingleConnection CreateConnectionToDbController(ServerId server,
            ConnectionConfiguration configuration, ConnectionTimeoutConfiguration time)
        {
            return new SingleConnectionToDbController(server, configuration, time);
        }


        public RemoteResult Process(ServerId server, InnerData data)
        {
            Logger.Logger.Instance.Debug(string.Format("DistributorNetModule: process server = {0}, data = {1}", server,
                                                       data.Transaction.EventHash));
            var connection = FindServer(server) as SingleConnectionToDbController;

            if (connection == null)
            {
                ConnectToDbController(server);
                connection = FindServer(server) as SingleConnectionToDbController;
            }

            if (connection == null)
            {
                Logger.Logger.Instance.Debug(string.Format(
                    "DistributorNetModule: process server not found  server = {0}, data = {1}", server,
                    data.Transaction.EventHash));
                _distributor.ServerNotAvailable(server);
                return new ServerNotFoundResult();
            }
            
            var ret = connection.ProcessData(data);

            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.Debug(string.Format("DbControllerNetModule: process fail result  server = {0}, data = {1}",
                                                           server,
                                                           data.Transaction.EventHash));

                RemoveConnection(server);
                _distributor.ServerNotAvailable(server);
            }

            return ret;
        }

        public RemoteResult Rollback(ServerId server, InnerData data)
        {
            Logger.Logger.Instance.Debug(string.Format("DistributorNetModule: rollback = {0}, data = {1}", server,
                                                           data.Transaction.EventHash));
            var connection = FindServer(server) as SingleConnectionToDbController;

            if (connection == null)
            {
                ConnectToDbController(server);
                connection = FindServer(server) as SingleConnectionToDbController;
            }

            if (connection == null)
            {
                _distributor.ServerNotAvailable(server);
                RemoveConnection(server);
                return new ServerNotFoundResult();
            }

            return connection.RollbackData(data);
        }

        public RemoteResult SendToDbController(ServerId server, NetCommand command)
        {
            Logger.Logger.Instance.TraceFormat("SendSync command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToDbController;

            if (connection == null)
            {
                ConnectToDbController(server);
                connection = FindServer(server) as SingleConnectionToDbController;
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
                _distributor.ServerNotAvailable(server);
            }

            return ret;
        }

        public InnerData ReadOperation(ServerId server, InnerData data)
        {
            Logger.Logger.Instance.Debug(string.Format("DistributorNetModule: process server = {0}, data = {1}", server,
                                                       data.Transaction.EventHash));
            var connection = FindServer(server) as SingleConnectionToDbController;

            if (connection == null)
            {
                ConnectToDbController(server);
                connection = FindServer(server) as SingleConnectionToDbController;
            }

            if (connection == null)
            {
                Logger.Logger.Instance.Debug(string.Format(
                    "DistributorNetModule: process server not found  server = {0}, data = {1}", server,
                    data.Transaction.EventHash));

                RemoveConnection(server);
                _distributor.ServerNotAvailable(server);
                return null;
            }
            
            RemoteResult res = null;
            var ret = connection.ReadOperation(data, out res);
            
            if (res is FailNetResult)
            {
                RemoveConnection(server);
                _distributor.ServerNotAvailable(server);
            }

            return ret;
        }

        #endregion

    }
}
