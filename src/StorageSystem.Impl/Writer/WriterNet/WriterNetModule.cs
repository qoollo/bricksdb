using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.Writer.Interfaces;
using Qoollo.Impl.Writer.WriterNet.Interfaces;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class WriterNetModule : NetModule, INetModule, IWriterNetModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public WriterNetModule(StandardKernel kernel, ConnectionConfiguration connectionConfiguration,
            ConnectionTimeoutConfiguration connectionTimeout)
            : base(kernel, connectionConfiguration, connectionTimeout)
        {
        }

        #region Connect to Writer

        public bool ConnectToWriter(ServerId server)
        {
            return ConnectToServer(server,
                (id, configuration, time) => new SingleConnectionToWriter(Kernel, id, configuration, time));
        }

        public void PingWriter(List<ServerId> servers)
        {
            PingServers(servers, (server) => { }, (server) => FindServer(server) as ICommonCommunicationNet,
                ConnectToWriter);
        }

        public RemoteResult SendToWriter(ServerId server, NetCommand command)
        {
            _logger.TraceFormat("SendSync command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToWriter;
            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToWriter;
            }

            if (connection == null)
            {
                _logger.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);
            if (ret is FailNetResult)
            {
                _logger.DebugFormat("WriterNetModule: process fail result  server: {0}, result: {1}", server, ret);
                RemoveConnection(server);
            }

            return ret;
        }        

        public RemoteResult ProcessSync(ServerId server, InnerData data)
        {
            var connection = FindServer(server) as SingleConnectionToWriter;

            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToWriter;
            }

            if (connection == null)
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug($"WriterNetModule: process server not found: {server}", "restore");

                return new ServerNotFoundResult();
            }
            var ret = connection.ProcessSync(data);

            if (ret is FailNetResult)
            {
                _logger.DebugFormat("WriterNetModule: process fail result  server: {0}, result: {1}", server, ret);
                RemoveConnection(server);
            }
            return ret;

        }

        public RemoteResult ProcessSync(ServerId server, List<InnerData> datas)
        {            
            var connection = FindServer(server) as SingleConnectionToWriter;

            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToWriter;
            }

            if (connection == null)
                return new ServerNotFoundResult();

            var ret = connection.ProcessSyncPackage(datas);

            if (ret is FailNetResult)
            {
                _logger.DebugFormat("WriterNetModule: process fail result  server: {0}, result: {1}", server, ret);
                RemoveConnection(server);
            }

            return ret;

        }

        public Task<RemoteResult> ProcessAsync(ServerId server, InnerData data)
        {
            var connection = FindServer(server) as SingleConnectionToWriter;

            if (connection == null)
            {
                ConnectToWriter(server);
                connection = FindServer(server) as SingleConnectionToWriter;
            }

            if (connection == null)
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug($"WriterNetModule: process server not found: {server}", "restore");

                var ret = new TaskCompletionSource<RemoteResult>();
                ret.SetResult(new ServerNotFoundResult());
                return ret.Task;
            }

            var finalTask = new TaskCompletionSource<RemoteResult>();
            var res = connection.ProcessTaskBased(data);
            res.ContinueWith(tsk =>
            {
                if (res.Result is FailNetResult)
                {
                    RemoveConnection(server);
                }
                finalTask.SetResult(res.Result);
            });

            return finalTask.Task;

        }


        #endregion

        #region Connect to distributor

        public bool ConnectToDistributor(ServerId server)
        {
            return ConnectToServer(server,
                (id, configuration, time) => new SingleConnectionToDistributor(Kernel, id, configuration, time));
        }

        public void PingDistributors(List<ServerId> servers)
        {
            PingServers(servers, (server) => { }, id => FindServer(id) as ICommonCommunicationNet, ConnectToDistributor);
        }

        //TODO посмотреть будет ли работать
        public RemoteResult SendToDistributor(NetCommand command)
        {
            _logger.TraceFormat("SendSync command {0}", command.GetType());
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
            _logger.TraceFormat("SendSync command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToDistributor;
            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                _logger.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return new ServerNotFoundResult();
            }

            var ret = connection.SendSync(command);
            if (ret is FailNetResult)
            {
                _logger.DebugFormat("WriterNetModule: process fail result  server: {0}, result: {1}", server, ret);
                RemoveConnection(server);
            }

            return ret;
        }

        public void ASendToDistributor(ServerId server, NetCommand command)
        {
            _logger.TraceFormat("SendSync command {0} to {1}", command.GetType(), server);
            var connection = FindServer(server) as SingleConnectionToDistributor;
            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                _logger.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return;
            }

            var ret = connection.SendASyncResult(command);
            if (ret is FailNetResult)
            {
                _logger.DebugFormat("WriterNetModule: process fail result  server: {0}, result: {1}", server, ret);
                RemoveConnection(server);
            }
        }

        public void TransactionAnswer(ServerId server, Transaction transaction)
        {
            var connection = FindServer(server) as SingleConnectionToDistributor;
            if (connection == null)
            {
                ConnectToDistributor(server);
                connection = FindServer(server) as SingleConnectionToDistributor;
            }

            if (connection == null)
            {
                _logger.DebugFormat("WriterNetModule: process server not found  server = {0}", server);
                return;
            }

            var ret = connection.TransactionAnswerResult(transaction);
            if (ret is FailNetResult)
            {
                _logger.DebugFormat("WriterNetModule: process fail result  server: {0}, result: {1}", server, ret);
                RemoveConnection(server);
            }
        }

        #endregion
    }
}
