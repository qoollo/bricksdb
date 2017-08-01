using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Commands;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy.Interfaces;
using Qoollo.Impl.Proxy.Model;

namespace Qoollo.Impl.Proxy
{
    internal class ProxyDistributorModule : ControlModule, IProxyDistributorModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly DistributorSystemModel _distributorSystemModel;
        private readonly AsyncTaskModule _async;
        private ServerId _local;
        private IProxyNetModule _net;
        private IGlobalQueue _queue;
        private IAsyncProxyCache _transactionCache;
        private IProxyConfiguration _config;

        public ServerId ProxyServerId => _local;

        public ProxyDistributorModule(StandardKernel kernel)
            : base(kernel)
        {
            _distributorSystemModel = new DistributorSystemModel();
            _async = new AsyncTaskModule(kernel);
        }

        public override void Start()
        {
            _config = Kernel.Get<IProxyConfiguration>();
            _local = _config.NetDistributor.ServerId;

            _queue = Kernel.Get<IGlobalQueue>();
            _transactionCache = Kernel.Get<IAsyncProxyCache>();
            _net = Kernel.Get<IProxyNetModule>();

            _async.Start();
            StartAsyncTasks();
            _queue.ProxyDistributorQueue.Registrate(Process);
        }

        public RemoteResult ProcessNetCommand(NetCommand command)
        {            
            _queue.ProxyDistributorQueue.Add(command);   
            return new SuccessResult();
        }        

        #region Private

        private void Process(NetCommand message)
        {
            if (message is ServerNotAvailableCommand)
                ServerNotAvailableInner(((ServerNotAvailableCommand) message).Server);

            if (message is OperationCompleteCommand)
            {
                CompleteOperation((message as OperationCompleteCommand).Transaction);
            }

            if (message is ReadOperationCompleteCommand)
            {
                CompleteOperation((message as ReadOperationCompleteCommand).Data);
            }
        }

        private void ServerNotAvailableInner(ServerId server)
        {
            _distributorSystemModel.ServerNotAvailable(server);
        }        

        private void CompleteOperation(Transaction transaction)
        {
            var data = _transactionCache.Get(transaction.CacheKey);
            if (data != null)
            {
                _transactionCache.Remove(transaction.CacheKey);
                data.UserSupportCallback.SetResult(transaction.UserTransaction);
            }
            else
            {
                _logger.Debug("Complete operation message income, but timeout");
            }
        }

        private void CompleteOperation(InnerData obj)
        {
            var data = _transactionCache.Get(obj.Transaction.CacheKey);
            if (data != null)
            {
                _transactionCache.Remove(obj.Transaction.CacheKey);
                data.InnerSupportCallback.SetResult(obj);
            }
            else
            {
                _logger.Debug("Complete read operation message income, but timeout");
            }
        }

        #endregion

        #region AsyncTasks

        private void StartAsyncTasks()
        {
            _async.AddAsyncTask(
                new AsyncDataPeriod(_config.Timeouts.DistributorUpdateInfoMls.PeriodTimeSpan,
                    TakeInfoFromAllDistributor, AsyncTasksNames.GetInfo, -1), false);

            _async.AddAsyncTask(
                new AsyncDataPeriod(_config.Timeouts.ServersPingMls.PeriodTimeSpan, PingProcess,
                    AsyncTasksNames.AsyncPing, -1), false);

        }

        private void PingProcess(AsyncData data)
        {
            var servers = _distributorSystemModel.GetDistributorsList();
            _net.PingDistributors(servers, _distributorSystemModel.ServerAvailable);
        }

        private void TakeInfoFromAllDistributor(AsyncData data)
        {
            var list = _distributorSystemModel.GetDistributorsList();
            list.ForEach(TakeInfoFromDistributor);
        }        

        #endregion        

        #region Connect actions
       
        public RemoteResult SayIAmHere(ServerId destination)
        {
            if (!_net.ConnectToDistributor(destination))
                return new ServerNotAvailable(destination);

            _distributorSystemModel.AddServer(destination);

            var ret = _net.SendDistributor(destination, new TakeInfoCommand());

            if (ret is SystemInfoResult)
            {
                var data = ret as SystemInfoResult;
                AddNewDistributorList(data.DataContainer.AllDistributors);
            }
            else
            {
                ret = new FailNetResult(Errors.InnerServerError);
            }

            return ret;
        }        

        private void TakeInfoFromDistributor(ServerId server)
        {
            if (_net.ConnectToDistributor(server))
            {
                var ret = _net.SendDistributor(server, new TakeInfoCommand());

                if (ret is SystemInfoResult)
                {
                    var data = ret as SystemInfoResult;
                    _distributorSystemModel.AddServers(data.DataContainer.AllDistributors);
                }
            }
        }

        private void AddNewDistributorList(List<ServerId> servers)
        {
            _distributorSystemModel.AddServers(servers);
            servers.ForEach(TakeInfoFromDistributor);
        }                

        #endregion

        #region Inner

        public Transaction CreateTransaction(string hash)
        {
            return _distributorSystemModel.CreateTransaction(hash);
        }

        public ServerId GetDestination(UserTransaction transaction)
        {
            return _distributorSystemModel.GetDestination(transaction);
        }

        public ServerId GetDestination()
        {
            return _distributorSystemModel.GetNewDestination();
        }

        public virtual void ServerNotAvailable(ServerId server)
        {
            _queue.ProxyDistributorQueue.Add(new ServerNotAvailableCommand(server));
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if(isUserCall)
                _async.Dispose();

            base.Dispose(isUserCall);
        }
    }
}
