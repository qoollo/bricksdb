using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class InitiatorRestoreModule : CommonAsyncWorkModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public ServerId RestoreServer => _serversController.RestoreServer;

        public List<ServerId> FailedServers => _serversController.FailedServers;

        public List<RestoreServer> Servers => _serversController.Servers;

        public InitiatorRestoreModule(RestoreModuleConfiguration configuration, WriterNetModule writerNet,
            AsyncTaskModule asyncTaskModule, RestoreStateHolder stateHolder, RestoreStateFileLogger saver)
            : base(writerNet, asyncTaskModule)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(stateHolder != null);
            _configuration = configuration;
            _stateHolder = stateHolder;            
            _serversController = new RestoreProcessController(saver);
        }

        private List<HashMapRecord> _local;        
        private readonly RestoreModuleConfiguration _configuration;
        private readonly RestoreStateHolder _stateHolder;
        private string _tableName;
        private RestoreState _state;
        private readonly RestoreProcessController _serversController;

        #region Restore start

        public void Restore(List<HashMapRecord> local, List<RestoreServer> servers, RestoreState state, string tableName)
        {
            if (ParametersCheck(local, state, tableName, servers))
                return;

            _serversController.SetServers(servers);

            StartRestore();
        }

        public void Restore(List<HashMapRecord> local, List<RestoreServer> servers, RestoreState state)
        {
            Restore(local, servers, state, Consts.AllTables);
        }

        public void RestoreFromFile(List<HashMapRecord> local, List<RestoreServer> servers, RestoreState state,
            string tableName)
        {
            if (ParametersCheck(local, state, tableName, servers))
                return;
            
            _serversController.SetServers(servers);

            StartRestore();
        }

        private bool ParametersCheck(List<HashMapRecord> local, RestoreState state, string tableName,
            IReadOnlyCollection<ServerId> servers)
        {
            Lock.EnterWriteLock();

            try
            {
                if (IsStartNoLock || !(servers.Count > 0 && local.Count > 0))
                    return true;

                _state = state;
                _tableName = tableName;
                IsStartNoLock = true;
                _local = local;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
            return false;
        }

        private void StartRestore()
        {
            _serversController.SetRestoreDate(_tableName, _state);            

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, NoAnswerCallback, AsyncTasksNames.RestoreRemote,
                    _configuration.CountRetry), false);

            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);

            _serversController.Save();
            CurrentProcess();
        }

        #endregion

        public void UpdateModel(List<ServerId> servers)
        {
            _serversController.UpdateModel(servers);   
        }

        private void CurrentProcess()
        {
            int result;
            do
            {
                result = ProcessNextServer();

                //TODO all servers are processed
                if (result == -1 && FailedServers.Count == 0)
                    FinishRestore();
                else if (result == -1)
                {
                    ProcessFailedServers();
                    result = 1;
                }

            } while (result == 1);

            if (result != -1)
                AsyncTaskModule.RestartTask(AsyncTasksNames.RestoreRemote);
        }

        private int ProcessNextServer()
        {
            var nextServer = _serversController.NextRestoreServer();
            if (nextServer == null)
                return -1;

            bool result = WriterNet.ConnectToWriter(nextServer);

            if(_logger.IsTraceEnabled)
                _logger.Trace($"Connection result = {result}", "restore");
            
            var state = nextServer.Equals(_local[0].ServerId)
                ? RestoreState.SimpleRestoreNeed
                : _state;
            var ret = WriterNet.SendToWriter(nextServer,
                new RestoreCommandWithData(_local[0].ServerId, _local.ToList(), _tableName, state));            

            if (ret is FailNetResult)
            {
                if (_logger.IsInfoEnabled)
                    _logger.InfoFormat($"Restore command for server: {nextServer} failed with result: {ret.Description}");

                _serversController.AddServerToFailed(nextServer);                
                return 1;
            }

            _serversController.RemoveCurrentServer();
            _serversController.SetCurrentServer(nextServer);                        
            _serversController.Save();

            return 0;
        }

        private void FinishRestore()
        {
            AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreRemote);
            IsStart = false;            
            _serversController.RemoveCurrentServer();

            if (_serversController.IsAllServersRestored())
            {
                _stateHolder.FinishRestore(_state);
                _serversController.FinishRestore();
            }

           _serversController.Save();

            if (_logger.IsInfoEnabled)
                _logger.Info("Restore current servers complete");
        }

        private void ProcessFailedServers()
        {
            _serversController.ProcessFailedServers();
            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);
        }

        #region Period events
        
        public void PeriodMessageIncome(ServerId server)
        {
            if (_logger.IsTraceEnabled)
                _logger.Trace($"period messge income from {server}", "restore");

            if (server.Equals(RestoreServer))
                AsyncTaskModule.RestartTask(AsyncTasksNames.RestoreRemote);
        }

        public void LastMessageIncome(ServerId server)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug($"last message income from {server}", "restore");

            if (server.Equals(RestoreServer))
            {
                _serversController.ServerRestored(server);
                CurrentProcess();
            }
        }

        private void NoAnswerCallback(AsyncData async)
        {
            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);

            var remoteServer = RestoreServer;

            if (async.IsLast() || remoteServer == null)
            {
                if (RestoreServer != null)
                    _serversController.AddServerToFailed(RestoreServer);
                CurrentProcess();
                return;
            }

            var state = remoteServer.Equals(_local[0].ServerId)
              ? RestoreState.SimpleRestoreNeed
              : _state;
            var ret = WriterNet.SendToWriter(remoteServer, new RestoreCommandWithData(_local[0].ServerId,
                _local.ToList(), _tableName, state));

            if (ret is FailNetResult)
            {
                if (RestoreServer != null)
                    _serversController.AddServerToFailed(RestoreServer);
                CurrentProcess();
            }
            else
                AsyncTaskModule.StartTask(AsyncTasksNames.RestoreRemote);
        }

        #endregion        

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);
            }

            base.Dispose(isUserCall);
        }
    }
}
