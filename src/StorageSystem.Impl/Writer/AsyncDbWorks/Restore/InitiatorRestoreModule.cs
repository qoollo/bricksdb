using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class InitiatorRestoreModule : CommonAsyncWorkModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public ServerId RestoreServer => _serversController.RestoreServer;

        public List<ServerId> FailedServers => _serversController.FailedServers;

        public List<RestoreServer> Servers => _serversController.Servers;

        public InitiatorRestoreModule(StandardKernel kernel, RestoreProcessController serversController)
            : base(kernel)
        {
            _serversController = serversController;
        }

        private IWriterModel _model;
        private string _tableName;
        private RestoreState _state;
        private readonly RestoreProcessController _serversController;
        private IWriterConfiguration _config;

        public override void Start()
        {
            base.Start();

            _model = Kernel.Get<IWriterModel>();
            _config = Kernel.Get<IWriterConfiguration>();
        }

        #region Restore start

        public void Restore(RestoreState state, string tableName)
        {
            if (ParametersCheck(state, tableName))
                return;

            StartRestore();
        }

        public void RestoreFromFile(RestoreState state, string tableName)
        {
            if (ParametersCheck(state, tableName))
                return;

            StartRestore();
        }

        private bool ParametersCheck(RestoreState state, string tableName)
        {
            Lock.EnterWriteLock();

            try
            {
                if (IsStartNoLock)
                    return true;

                _state = state;
                _tableName = tableName;
                IsStartNoLock = true;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
            return false;
        }

        private void StartRestore()
        {
            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_config.Restore.Initiator.PeriodRetryMls, NoAnswerCallback,
                    AsyncTasksNames.RestoreRemote, _config.Restore.Initiator.CountRetry), false);

            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);

            CurrentProcess();
        }

        #endregion

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
            
            var state = nextServer.Equals(_model.Local)
                ? RestoreState.SimpleRestoreNeed
                : _state;
            var ret = WriterNet.SendToWriter(nextServer,
                new RestoreCommandWithData(_model.Local, _tableName, state));            

            if (ret is FailNetResult)
            {
                if (_logger.IsInfoEnabled)
                    _logger.InfoFormat($"Restore command for server: {nextServer} failed with result: {ret.Description}");

                _serversController.AddServerToFailed(nextServer);                
                return 1;
            }

            _serversController.RemoveCurrentServer();
            _serversController.SetCurrentServer(nextServer);                        

            return 0;
        }

        private void FinishRestore()
        {
            AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreRemote);
            IsStart = false;            
            _serversController.RemoveCurrentServer();

            _serversController.FinishRestore();

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

            var state = remoteServer.Equals(_model.Local)
              ? RestoreState.SimpleRestoreNeed
              : _state;
            var ret = WriterNet.SendToWriter(remoteServer,
                new RestoreCommandWithData(_model.Local, _tableName, state));

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

        public InitiatorStateDataContainer GetState()
        {
            if (IsStart)
                return new InitiatorStateDataContainer(RestoreServer);
            return null;
        }

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
