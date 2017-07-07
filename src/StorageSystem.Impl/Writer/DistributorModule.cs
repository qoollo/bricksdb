using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.TestSupport;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer
{
    internal class DistributorModule : ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly WriterModel _model;
        private readonly WriterNetModule _writerNet;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly AsyncDbWorkModule _asyncDbWork;
        private readonly IGlobalQueue _queue;

        public DistributorModule(StandardKernel kernel,
            WriterModel model, 
            AsyncTaskModule async, 
            AsyncDbWorkModule asyncDbWork,
            WriterNetModule writerNet,
            QueueConfiguration configuration,
            AsyncTasksConfiguration pingConfiguration = null)
            :base(kernel)
        {
            Contract.Requires(writerNet != null);
            Contract.Requires(configuration != null);
            Contract.Requires(asyncDbWork != null);
            Contract.Requires(async != null);

            _asyncDbWork = asyncDbWork;
            _model = model;
            _writerNet = writerNet;
            _queueConfiguration = configuration;

            _queue = kernel.Get<IGlobalQueue>();

            var ping = InitInjection.PingPeriod;

            if (pingConfiguration != null)
                ping = pingConfiguration.TimeoutPeriod;

            async.AddAsyncTask(
                new AsyncDataPeriod(ping, Ping, AsyncTasksNames.AsyncPing, -1), false);
        }

        public override void Start()
        {
            _model.Start();
            _asyncDbWork.Start();            
            RegistrateCommands();
        }

        private void RegistrateCommands()
        {
            RegistrateAsync<Transaction, Transaction, RemoteResult>(_queue.TransactionAnswerQueue, ProcessTransaction,
               () => new SuccessResult());

            RegistrateAsync<RestoreFromDistributorCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                _asyncDbWork.Restore, () => new SuccessResult());

            RegistrateAsync<RestoreCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                _asyncDbWork.Restore, () => new SuccessResult());

            RegistrateAsync<RestoreInProcessCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                command => _asyncDbWork.PeriodMessageIncome(command.ServerId), () => new SuccessResult());

            RegistrateAsync<RestoreCompleteCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                command => _asyncDbWork.LastMessageIncome(command.ServerId), () => new SuccessResult());

            RegistrateAsync<RestoreCommandWithData, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                comm => _asyncDbWork.RestoreIncome(comm.ServerId, comm.RestoreState, comm.TableName),
                () => new SuccessResult());

            RegistrateAsync<DeleteCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue, DeleteCommand,
                () => new SuccessResult());

            RegistrateSync<SetGetRestoreStateCommand, SetGetRestoreStateResult>(
                command => new SetGetRestoreStateResult(
                    _asyncDbWork.DistributorReceive(command.State),
                    GetAllStateDict()));

            RegistrateSync<HashFileUpdateCommand, RemoteResult>(HashFileUpdate);

            StartAsync(_queueConfiguration);
        }

        private void Ping(AsyncData data)
        {
            var servers = _writerNet.GetServersByType(typeof (SingleConnectionToDistributor));
            _writerNet.PingDistributors(servers);

            servers = _writerNet.GetServersByType(typeof(SingleConnectionToWriter));
            _writerNet.PingWriter(servers);

            //remove old connections after model update
            var map = _model.Servers;
            servers = servers.Where(x => !map.Contains(x)).ToList();
            servers.ForEach(x => _writerNet.RemoveConnection(x));
        }

        #region to user

        public string UpdateModel()
        {
            if (!_asyncDbWork.IsRestoreStarted)
            {
                _model.UpdateModel();
                _asyncDbWork.UpdateModel();

                return Errors.NoErrors;
            }
            return Errors.RestoreAlreadyStarted;
        }

        public string Restore()
        {
            return Restore(null, RestoreState.Default);
        }

        public string Restore(RestoreState state)
        {
            return Restore(null, state);
        }

        public string Restore(RestoreState state, RestoreType type)
        {
            return Restore(null, state, type);
        }

        public string Restore(RestoreType type)
        {
            return Restore(null, RestoreState.Default, type);
        }

        public string Restore(List<ServerId> servers, RestoreState state, RestoreType type = RestoreType.Single)
        {
            if (_asyncDbWork.IsRestoreStarted)
                return Errors.RestoreAlreadyStarted;

            RestoreState st = state;
            if (state == RestoreState.Default && type == RestoreType.Single)
            {
                st = _asyncDbWork.RestoreState;
                if (st == RestoreState.Restored)
                    return Errors.RestoreDefaultStartError;
            }

            Execute<RestoreCommand, RemoteResult>(new RestoreCommand(st, type)
            {
                DirectServers = servers
            });

            return Errors.RestoreStartedWithoutErrors;
        }

        public bool IsRestoreCompleted()
        {
            return !_asyncDbWork.IsRestoreStarted;
        }

        public List<ServerId> FailedServers()
        {
            return _asyncDbWork.GetFailedServers();
        }

        public RestoreState GetRestoreRequiredState()
        {
            return _asyncDbWork.RestoreState;
        }

        public string GetAllState()
        {
            return _asyncDbWork.GetAllState();
        }

        private string GetServersList(string start = "\n")
        {
            return _asyncDbWork.Servers.Aggregate(start, (current, server) => current + $"\t{server}\n");
        }

        private Dictionary<string, string> GetAllStateDict()
        {
            var ret = _asyncDbWork.FullState;
            if (_asyncDbWork.RestoreState != RestoreState.Restored)
                ret.Add(ServerState.RestoreServers, GetServersList());
            return ret;
        }

        public string DisableDelete()
        {
            _asyncDbWork.TimeoutModule.Disable();
            return Errors.NoErrors;
        }

        public string EnableDelete()
        {
            _asyncDbWork.TimeoutModule.Enable();
            return Errors.NoErrors;
        }

        public string StartDelete()
        {
            _asyncDbWork.TimeoutModule.StartDelete();
            return Errors.NoErrors;
        }

        public string RunDelete()
        {
            _asyncDbWork.TimeoutModule.RunDelete();
            return Errors.NoErrors;
        }

        #endregion

        #region Commands

        private void DeleteCommand(DeleteCommand command)
        {
            switch (command.Command.ToLower())
            {
                case "disable":
                    DisableDelete();
                    break;
                case "enable":
                    EnableDelete();
                    break;
                case "start":
                    StartDelete();
                    break;
                case "run":
                    RunDelete();
                    break;
            }
        }

        private void ProcessTransaction(Transaction transaction)
        {
            if (transaction.Distributor != null)
                _writerNet.TransactionAnswer(transaction.Distributor, transaction);
        }

        private RemoteResult HashFileUpdate(HashFileUpdateCommand command)
        {
            if (_asyncDbWork.IsRestoreStarted)
                return new InnerFailResult("Restore process is started");

            var result = _model.UpdateHashViaNet(command.Map);
            RemoteResult ret;

            if (result == string.Empty)
            {
                ret = new SuccessResult();
                _asyncDbWork.UpdateModel();
            }
            else
                ret = new InnerFailResult(result);

            return ret;
        }

        #endregion

        public bool IsMine(string hash)
        {
            return _model.IsMine(hash);
        }
    }
}
