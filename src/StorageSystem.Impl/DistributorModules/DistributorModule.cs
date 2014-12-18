using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Commands;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.System.Collector;
using Qoollo.Impl.Common.NetResults.System.DbController;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Model;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.DistributorModules
{
    internal class DistributorModule : ControlModule
    {
        private readonly ControllerSystemModel _modelOfDbControllers;
        private readonly DistributorSystemModel _modelOfAnotherDistributors;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly DistributorNetModule _distributorNet;
        private readonly ServerId _localfordb;
        private readonly ServerId _localforproxy;
        private readonly GlobalQueueInner _queue;
        private readonly AsyncTaskModule _asyncTaskModule;
        private readonly AsyncTasksConfiguration _asyncPing;
        private readonly AsyncTasksConfiguration _asyncCheck;

        public ServerId LocalForDb
        {
            get { return _localfordb; }
        }

        public DistributorModule(
            AsyncTasksConfiguration asyncPing,
            AsyncTasksConfiguration asyncCheck,
            DistributorHashConfiguration configuration,
            QueueConfiguration queueConfiguration,
            DistributorNetModule distributorNet,
            ServerId localfordb,
            ServerId localforproxy,
            HashMapConfiguration hashMapConfiguration)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(queueConfiguration != null);
            Contract.Requires(distributorNet != null);
            Contract.Requires(localfordb != null);
            Contract.Requires(localforproxy != null);
            Contract.Requires(asyncPing != null);
            _asyncPing = asyncPing;
            _asyncTaskModule = new AsyncTaskModule(queueConfiguration);

            _queueConfiguration = queueConfiguration;
            _modelOfDbControllers = new ControllerSystemModel(configuration, hashMapConfiguration);
            _modelOfAnotherDistributors = new DistributorSystemModel();
            _distributorNet = distributorNet;
            _localfordb = localfordb;
            _localforproxy = localforproxy;
            _asyncCheck = asyncCheck;
            _queue = GlobalQueue.Queue;
        }

        public override void Start()
        {
            _queue.DistributorDistributorQueue.Registrate(_queueConfiguration, Process);
            _queue.DistributorTransactionCallbackQueue.Registrate(_queueConfiguration, ProcessCallbackTransaction);
            _modelOfDbControllers.Start();

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_asyncPing.TimeoutPeriod, PingProcess, AsyncTasksNames.AsyncPing, -1), false);

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_asyncCheck.TimeoutPeriod, CheckRestore, AsyncTasksNames.CheckRestore, -1), false);

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_asyncCheck.TimeoutPeriod, DistributerPing, AsyncTasksNames.CheckDistributors, -1),
                false);

            _asyncTaskModule.Start();
        }

        private void ProcessCallbackTransaction(Common.Data.TransactionTypes.Transaction transaction)
        {
            _distributorNet.ASendToProxy(transaction.ProxyServerId, new OperationCompleteCommand(transaction));
        }

        #region Private

        private HashMapResult GetHashMap()
        {
            return new HashMapResult(_modelOfDbControllers.GetAllServers());
        }

        private void RestoreServerCommand(RestoreCommand command)
        {
            //TODO тоже надо будет выпилить
            command.CountSends++;

            _distributorNet.ConnectToDbController(command.RestoreServer);
            _modelOfDbControllers.ServerAvailable(command.RestoreServer);

            if (command.CountSends < 2)
            {
                var list = _modelOfAnotherDistributors.GetDistributorList();
                list.ForEach(x => _distributorNet.SendToDistributor(x, command));
            }
        }

        private void ServerNotAvailableInner(ServerId server)
        {
            Logger.Logger.Instance.Debug("Distributor: Server not available " + server);
            _modelOfDbControllers.ServerNotAvailable(server);
        }

        private void PingProcess(AsyncData data)
        {
            var map = _modelOfDbControllers.GetAllServers2();
            _distributorNet.PingDbControllers(map, _modelOfDbControllers.ServerAvailable);

            map = _distributorNet.GetServersByType(typeof (SingleConnectionToProxy));
            _distributorNet.PingProxy(map);

            map = _distributorNet.GetServersByType(typeof (SingleConnectionToDistributor));
            _distributorNet.PingDistributors(map);
        }

        private void CheckRestore(AsyncData data)
        {
            var map = _modelOfDbControllers.GetAllServers2();
            map.ForEach(x =>
            {
                var result = _distributorNet.SendToDbController(x, new IsRestoredCommand());

                if (result is IsRestoredResult && ((IsRestoredResult) result).IsRestored)
                {
                    _modelOfDbControllers.ServerIsRestored(x);
                }
            });
        }

        private void DistributerPing(AsyncData data)
        {
            var servers = _modelOfAnotherDistributors.GetDistributorList();
            AddDistributors(servers);
        }

        #endregion

        #region To main logic

        public bool IsSomethingHappendInSystem()
        {
            return _modelOfDbControllers.IsSomethingHappendInSystem();
        }

        public List<ControllerDescription> GetDestination(InnerData data, bool needAllServers)
        {
            Logger.Logger.Instance.Trace(
                string.Format("Distributor: Get destination event hash = {0}, distr hash = {1}",
                    data.Transaction.EventHash, data.Transaction.DistributorHash));

            data.Transaction.IsNeedAllServes = needAllServers;

            List<ControllerDescription> ret = null;
            if (!needAllServers)
                ret = _modelOfDbControllers.GetDestination(data);
            else
                ret = _modelOfDbControllers.GetAllAvailableServers();

            if (ret.Count == 0)
                return null;
            return ret;
        }

        #endregion

        #region Public

        public void ServerNotAvailable(ServerId server)
        {
            _queue.DistributorDistributorQueue.Add(new ServerNotAvailableCommand(server));
        }

        public RemoteResult ProcessNetCommand(NetCommand command)
        {
            if (command is GetHashMapCommand)
                return GetHashMap();

            if (command is TakeInfoCommand)
                return GetDistributorInfo();

            _queue.DistributorDistributorQueue.Add(command);
            return new SuccessResult();
        }

        private void Process(NetCommand message)
        {
            if (message is ServerNotAvailableCommand)
                ServerNotAvailableInner((message as ServerNotAvailableCommand).Server);

            else if (message is AddDistributorFromDistributorCommand)
                AddDistributor(message as AddDistributorFromDistributorCommand);

            else if (message is RestoreCommand)
                RestoreServerCommand(message as RestoreCommand);
            else
                Logger.Logger.Instance.ErrorFormat("Not supported command type = {0}", message.GetType());
        }

        public RemoteResult ProcessTransaction(Common.Data.TransactionTypes.Transaction transaction)
        {
            _queue.TransactionQueue.Add(transaction);
            return new SuccessResult();
        }

        public void UpdateModel()
        {
            _modelOfDbControllers.UpdateFromFile();
        }

        #endregion

        #region Distributor communication

        public RemoteResult SayIAmHereRemoteResult(ServerId destination)
        {
            if (!_distributorNet.ConnectToDistributor(destination))
                return new ServerNotAvailable(destination);

            var ret = _distributorNet.SendToDistributor(destination,
                new AddDistributorFromDistributorCommand(new ServerId(_localforproxy)));

            if (!ret.IsError)
            {
                _modelOfAnotherDistributors.AddServer(destination);
                ret = _distributorNet.SendToDistributor(destination, new TakeInfoCommand());

                if (!ret.IsError)
                {
                    var data = ret as SystemInfoResult;
                    AddDistributors(data.DataContainer.AllDistributors);
                }
            }
            return ret;
        }

        private void AddDistributors(List<ServerId> distributors)
        {
            for (int i = 0; i < distributors.Count; i++)
            {
                if (!distributors[i].Equals(_localforproxy) &&
                    !_modelOfAnotherDistributors.GetDistributorList().Contains(distributors[i]) &&
                    _distributorNet.ConnectToDistributor(distributors[i]))
                {
                    _modelOfAnotherDistributors.AddServer(distributors[i]);
                    _distributorNet.SendToDistributor(distributors[i],
                        new AddDistributorFromDistributorCommand(new ServerId(_localforproxy)));

                    var ret = _distributorNet.SendToDistributor(distributors[i], new TakeInfoCommand());

                    if (!ret.IsError)
                    {
                        var data = ret as SystemInfoResult;
                        distributors.AddRange(data.DataContainer.AllDistributors);
                    }
                }
            }
        }

        private void AddDistributor(AddDistributorFromDistributorCommand command)
        {
            if (_distributorNet.ConnectToDistributor(command.Server))
                _modelOfAnotherDistributors.AddServer(command.Server);
        }

        private SystemInfoResult GetDistributorInfo()
        {
            var data = new DistributorDataContainer(_modelOfAnotherDistributors.GetDistributorList());
            return new SystemInfoResult(data);
        }

        #endregion

        #region Test

        public List<ServerId> GetDistributors()
        {
            return _modelOfAnotherDistributors.GetDistributorList();
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _asyncTaskModule.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }
}
