using System.Collections.Generic;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.Writer.WriterNet.Interfaces;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class WriterNetModule:NetModule, INetModule
    {
        public WriterNetModule(ConnectionConfiguration connectionConfiguration,
            ConnectionTimeoutConfiguration connectionTimeout)
            : base(connectionConfiguration, connectionTimeout)
        {
        }

        #region Connect to Writer

        public bool ConnectToWriter(ServerId server)
        {
            return ConnectToServer(server,
                (id, configuration, time) => new SingleConnectionToWriter(id, configuration, time));
        }

        public void PingWriter(List<ServerId> servers)
        {
            PingServers(servers, (server) => { }, (server) => FindServer(server) as ICommonCommunicationNet,
                ConnectToWriter);
        }

        public RemoteResult SendToWriter(ServerId server, NetCommand command)
        {
            Logger.Logger.Instance.TraceFormat("SendSync command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToWriter;
            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToWriter;
            }

            if (connection == null)
            {
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);
            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process fail result  server = {0}", server);
                RemoveConnection(server);
            }

            return ret;
        }        

        public RemoteResult ProcessSync(ServerId server, InnerData data)
        {
            Logger.Logger.Instance.Debug(string.Format("WriterNetModule: process server = {0}, ev = {1}", server,
                                           data.Transaction.EventHash));
            var connection = FindServer(server) as SingleConnectionToWriter;

            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToWriter;
            }

            if (connection == null)
            {
                Logger.Logger.Instance.Debug(string.Format(
                    "WriterNetModule: process server not found  server = {0}, ev = {1}", server,
                    data.Transaction.EventHash), "restore");
                return new ServerNotFoundResult();
            }
            var ret = connection.ProcessSync(data);

            if (ret is FailNetResult)
            {
                RemoveConnection(server);
            }
            return ret;

        }

        #endregion

        #region Connect to distributor

        public bool ConnectToDistributor(ServerId server)
        {
            return ConnectToServer(server,
                (id, configuration, time) => new SingleConnectionToDistributor(id, configuration, time));
        }

        public void PingDistributors(List<ServerId> servers)
        {
            PingServers(servers, (server) => { }, id => FindServer(id) as ICommonCommunicationNet, ConnectToDistributor);
        }

        //TODO посмотреть будет ли работать
        public RemoteResult SendToDistributor(NetCommand command)
        {
            Logger.Logger.Instance.TraceFormat("SendSync command {0}", command.GetType());
            var connection = FindServer<SingleConnectionToDistributor>() as SingleConnectionToDistributor;            

            if (connection == null)
            {                
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);

            return ret;
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
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);
            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process fail result  server = {0}", server);
                RemoveConnection(server);
            }

            return ret;
        }

        public void ASendToDistributor(ServerId server, NetCommand command)
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
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return;
            }

            var ret = connection.SendASyncResult(command);
            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process fail result  server = {0}", server);
                RemoveConnection(server);
            }
        }

        public void TransactionAnswer(ServerId server, Transaction transaction)
        {
            Logger.Logger.Instance.TraceFormat("Transaction command {0} to {1}", transaction.CacheKey, server);
            var connection = FindServer(server) as SingleConnectionToDistributor;
            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return;
            }

            var ret = connection.TransactionAnswerResult(transaction);
            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("WriterNetModule: process fail result  server = {0}", server);
                RemoveConnection(server);
            }
        }

        #endregion
    }
}
