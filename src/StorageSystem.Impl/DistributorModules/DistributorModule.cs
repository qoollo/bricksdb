using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Commands;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.System.Collector;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.DistributorModules.Model;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.DistributorModules
{
    internal class DistributorModule : ControlModule, IDistributorModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public ServerId LocalForDb
        {
            get { return _localfordb; }
        }

        public DistributorModule(
            StandardKernel kernel,
            AsyncTasksConfiguration asyncPing,
            AsyncTasksConfiguration asyncCheck,
            ServerId localfordb,
            ServerId localforproxy,
            HashMapConfiguration hashMapConfiguration, bool autoRestoreEnable = false)
            :base(kernel)
        {
            Contract.Requires(localfordb != null);
            Contract.Requires(localforproxy != null);
            Contract.Requires(asyncPing != null);
            _asyncPing = asyncPing;
            _asyncTaskModule = new AsyncTaskModule(kernel);

            _modelOfAnotherDistributors = new DistributorSystemModel();
            _localfordb = localfordb;
            _localforproxy = localforproxy;
            _hashMapConfiguration = hashMapConfiguration;
            _autoRestoreEnable = autoRestoreEnable;
            _asyncCheck = asyncCheck;
        }

        private WriterSystemModel _modelOfDbWriters;
        private IGlobalQueue _queue;
        private IDistributorNetModule _distributorNet;
        private readonly DistributorSystemModel _modelOfAnotherDistributors;
        private readonly ServerId _localfordb;
        private readonly ServerId _localforproxy;
        private readonly HashMapConfiguration _hashMapConfiguration;
        private readonly AsyncTaskModule _asyncTaskModule;
        private readonly AsyncTasksConfiguration _asyncPing;
        private readonly AsyncTasksConfiguration _asyncCheck;
        private bool _autoRestoreEnable;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public override void Start()
        {
            _distributorNet = Kernel.Get<IDistributorNetModule>();
            _queue = Kernel.Get<IGlobalQueue>();

            var config = Kernel.Get<ICommonConfiguration>();
            _modelOfDbWriters = new WriterSystemModel(_hashMapConfiguration, config.CountReplics);

            RegistrateCommands();
      
            _modelOfDbWriters.Start();

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_asyncPing.TimeoutPeriod, PingProcess, AsyncTasksNames.AsyncPing, -1), false);

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_asyncCheck.TimeoutPeriod, CheckRestore, AsyncTasksNames.CheckRestore, -1), false);

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_asyncCheck.TimeoutPeriod, DistributerPing, AsyncTasksNames.CheckDistributors, -1),
                false);

            _asyncTaskModule.Start();

            StartAsync();
        }

        private void RegistrateCommands()
        {
            RegistrateSync<GetHashMapCommand, RemoteResult>(GetHashMap);
            RegistrateSync<TakeInfoCommand, RemoteResult>(GetDistributorInfo);

            RegistrateAsync<ServerNotAvailableCommand, NetCommand, RemoteResult>(_queue.DistributorDistributorQueue,
                ServerNotAvailableInner, () => new SuccessResult());

            RegistrateAsync<AddDistributorFromDistributorCommand, NetCommand, RemoteResult>(
                _queue.DistributorDistributorQueue,AddDistributor, () => new SuccessResult());

            RegistrateAsync<HashFileUpdateCommand, NetCommand, RemoteResult>(
                _queue.DistributorDistributorQueue,
                command => _modelOfDbWriters.UpdateHashViaNet(command.Map), () => new SuccessResult());

            RegistrateAsync<NetCommand, NetCommand, RemoteResult>(
                _queue.DistributorDistributorQueue,
                command => _logger.InfoFormat("Not supported command type = {0}", command.GetType()),
                () => new SuccessResult());

            RegistrateAsync<Common.Data.TransactionTypes.Transaction,
                Common.Data.TransactionTypes.Transaction, RemoteResult>(
                    _queue.DistributorTransactionCallbackQueue, ProcessCallbackTransaction,
                    () => new SuccessResult());
        }

        private void ProcessCallbackTransaction(Common.Data.TransactionTypes.Transaction transaction)
        {
            if (_distributorNet != null)
                _distributorNet.ASendToProxy(transaction.ProxyServerId, new OperationCompleteCommand(transaction));
        }

        #region Private

        private HashMapResult GetHashMap()
        {
            return new HashMapResult(_modelOfDbWriters.GetAllServersForCollector());
        }

        private void ServerNotAvailableInner(ServerNotAvailableCommand command)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Distributor: Server not available " + command.Server);
            _modelOfDbWriters.ServerNotAvailable(command.Server);
        }

        private void PingProcess(AsyncData data)
        {
            try
            {
                var map = _modelOfDbWriters.GetAllServers2();
                _distributorNet.PingWriters(map, _modelOfDbWriters.ServerAvailable);

                map = _distributorNet.GetServersByType(typeof(SingleConnectionToProxy));
                _distributorNet.PingProxy(map);

                map = _distributorNet.GetServersByType(typeof(SingleConnectionToDistributor));
                _distributorNet.PingDistributors(map);

                //remove old connections after model update
                //var real = _distributorNet.GetServersByType(typeof(SingleConnectionToWriter));
                //real = real.Where(x => !map.Contains(x)).ToList();
                //real.ForEach(x => _distributorNet.RemoveConnection(x));
            }
            catch (Exception)
            {
            }
            
        }

        private void CheckRestore(AsyncData data)
        {
            CheckRestoreInner(true);
        }

        private void CheckRestoreInner(bool isRestore = false)
        {
            var servers = _modelOfDbWriters.GetAllAvailableServers();
            servers.ForEach(x =>
            {
                var result = _distributorNet.SendToWriter(x, new SetGetRestoreStateCommand(x.RestoreState));

                if (result is SetGetRestoreStateResult)
                {
                    var command = ((SetGetRestoreStateResult)result);
                    x.UpdateState(command.State);
                    x.SetInfoMessageList(command.FullState);
                }
            });

            _lock.EnterReadLock();
            var autoRestore = isRestore && _autoRestoreEnable;
            _lock.ExitReadLock();

            if (autoRestore)
                RestoreWriters(servers);
        }


        private void RestoreWriters(List<WriterDescription> servers)
        {
            if (!servers.All(x => x.IsAvailable && !x.IsRestoreInProcess))
                return;
            var server = servers.FirstOrDefault(x => x.RestoreState == RestoreState.SimpleRestoreNeed);
            if (server != null)
            {
                _distributorNet.SendToWriter(server, new RestoreFromDistributorCommand());
            }
        }

        private void DistributerPing(AsyncData data)
        {
            var servers = _modelOfAnotherDistributors.GetDistributorList();
            AddDistributors(servers);
        }

        private List<WriterDescription> UpdateWritersAsync(List<WriterDescription> servers, HashFileUpdateCommand command)
        {
            var ret = new List<WriterDescription>();
            servers.ForEach(x =>
            {
                var result = _distributorNet.SendToWriter(x,command);
                if (result is InnerFailResult)
                {
                    var message = ((InnerFailResult)result).Description;
                    x.SetInfoMessage(ServerState.Update, message);
                    _logger.ErrorFormat("Hash update fail. Server: {0}, Message: {1}", x, message);

                }

                if (!result.IsError)
                {
                    ret.Add(x);
                    x.SetInfoMessage(ServerState.Update, string.Empty);
                }
            });
            return ret;
        }

        private List<ServerId> UpdateDistributorsAsync(List<ServerId> servers, HashFileUpdateCommand command)
        {
            var ret = new List<ServerId>();
            servers.ForEach(x =>
            {
                var result = _distributorNet.SendToDistributor(x, command);
                if (!result.IsError)
                    ret.Add(x);
            });
            return ret;
        }

        private void UpdateWritersAndDistributors()
        {
            var writers = _modelOfDbWriters.Servers;
            var map = _modelOfDbWriters.GetHashMap();
            var command = new HashFileUpdateCommand(map);

            var failedWriters = new List<WriterDescription>();
            writers.ForEach(x =>
            {
                var result = _distributorNet.SendToWriter(x, command);
                if (result.IsError)
                {
                    failedWriters.Add(x);
                    if (result is InnerFailResult)
                    {
                        var message = ((InnerFailResult)result).Description;
                        x.SetInfoMessage(ServerState.Update, message);
                        _logger.ErrorFormat("Hash update fail. Server: {0}, Message: {1}", x, message);

                    }
                }
                else
                    x.SetInfoMessage(ServerState.Update, string.Empty);
            });

            if (failedWriters.Count != 0)
            {
                _asyncTaskModule.AddAsyncTask(new AsyncDataPeriod(_asyncCheck.TimeoutPeriod,
                    data =>
                    {
                        var list = UpdateWritersAsync(failedWriters, command);
                        failedWriters.RemoveAll(x => list.Contains(x));
                    }, AsyncTasksNames.UpdateHashFileForWriter, -1), false);
                _asyncTaskModule.StartTask(AsyncTasksNames.UpdateHashFileForWriter);
            }

            var distributors = _modelOfAnotherDistributors.GetDistributorList();
            var failedDistributors = new List<ServerId>();
            distributors.ForEach(x =>
            {
                var result = _distributorNet.SendToDistributor(x, command);
                if (result.IsError)
                    failedDistributors.Add(x);
            });

            if (failedDistributors.Count != 0)
            {
                _asyncTaskModule.AddAsyncTask(new AsyncDataPeriod(_asyncCheck.TimeoutPeriod,
                    data =>
                    {
                        var list = UpdateDistributorsAsync(failedDistributors, command);
                        failedDistributors.RemoveAll(x => list.Contains(x));
                    }, AsyncTasksNames.UpdateHashFileForDistributor, -1), false);
                _asyncTaskModule.StartTask(AsyncTasksNames.UpdateHashFileForDistributor);
            }
        }

        #endregion

        #region To main logic

        public bool IsSomethingHappendInSystem()
        {
            return _modelOfDbWriters.IsSomethingHappendInSystem();
        }

        public List<WriterDescription> GetDestination(InnerData data, bool needAllServers)
        {
            var ret = !needAllServers
                ? _modelOfDbWriters.GetDestination(data)
                : _modelOfDbWriters.GetAllAvailableServers();

            return ret.Count == 0 ? null : ret;
        }

        #endregion

        #region Public

        public void ServerNotAvailable(ServerId server)
        {
            Execute<ServerNotAvailableCommand, RemoteResult>(new ServerNotAvailableCommand(server));
        }

        public new TResult Execute<TValue, TResult>(TValue value) where TValue : class
        {
            return base.Execute<TValue, TResult>(value);
        }

        public void ProcessTransaction(Common.Data.TransactionTypes.Transaction transaction)
        {
            _queue.TransactionQueue.Add(transaction);            
        }       

        #endregion

        #region Command from User

        public string DeleteMode(string mode)
        {
            mode = mode.ToLower();
            if (mode != "start" && mode != "disable" && mode != "enable" && mode != "run" || string.IsNullOrEmpty(mode))
                return $"Value {mode} is not recognized. Use start, disable, enable, run";

            var servers = _modelOfDbWriters.GetAllAvailableServers();
            var command = new DeleteCommand(mode);
            servers.ForEach(x => _distributorNet.SendToWriter(x, command));

            return "Ok";
        }

        public string UpdateModel()
        {
            _modelOfDbWriters.UpdateFromFile();
            _modelOfDbWriters.UpdateModel();

            _asyncTaskModule.DeleteTask(AsyncTasksNames.UpdateHashFileForDistributor);
            _asyncTaskModule.DeleteTask(AsyncTasksNames.UpdateHashFileForWriter);

            UpdateWritersAndDistributors();

            return Errors.NoErrors;
        }

        public string GetServersState()
        {
            var servers = _modelOfDbWriters.GetAllAvailableServers();
            if (servers.TrueForAll(x => x.IsAvailable))
                CheckRestoreInner();
            return _modelOfDbWriters.Servers.Aggregate(string.Empty,
                (current, writerDescription) => current + "\n" + writerDescription.StateString);
        }

        public List<ServerId> GetDistributors()
        {
            return _modelOfAnotherDistributors.GetDistributorList();
        }

        public string AutoRestoreSetMode(bool mode)
        {            
            _lock.EnterWriteLock();
            _autoRestoreEnable = mode;
            _lock.ExitWriteLock();

            return Errors.NoErrors;
        }

        public string Restore(ServerId server, ServerId restoreDest, RestoreState state)
        {
            if (!_modelOfDbWriters.Servers.Contains(server))
                return "non existed server";

            var firstOrDefault = _modelOfDbWriters.Servers.FirstOrDefault(x => Equals(x, server));
            if ((state == RestoreState.Default && firstOrDefault.RestoreState == RestoreState.Restored))
                return $"server {restoreDest} is in restore mode. Change restore mode";
            var result = _distributorNet.SendToWriter(server, new RestoreFromDistributorCommand(state, restoreDest));

            return result.IsError ? result.ToString() : Errors.NoErrors;
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
