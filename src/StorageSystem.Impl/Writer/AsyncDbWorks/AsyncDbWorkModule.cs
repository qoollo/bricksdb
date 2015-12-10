using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.TestSupport;
using Qoollo.Impl.Writer.AsyncDbWorks.Restore;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Timeout;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class AsyncDbWorkModule:ControlModule
    {        
        public RestoreState RestoreState
        {
            get { return _stateHolder.State; }
        }

        public TimeoutModule TimeoutModule { get { return _timeout; } }

        internal bool IsNeedRestore
        {
            get { return _stateHolder.State != RestoreState.Restored; }
        }

        public bool IsRestoreStarted
        {
            get
            {
                return _initiatorRestore.IsStart;
            }
        }

        public bool IsTransferRestoreStarted
        {
            get
            {
                return _transferRestore.IsStart;
            }
        }

        public Dictionary<string, string> FullState
        {
            get
            {
                var dictionary = new Dictionary<string, string>();

                if (_initiatorRestore.IsStart)
                {
                    var server = _initiatorRestore.RestoreServer;
                    if (server != null)
                        dictionary.Add(ServerState.RestoreCurrentServer, server.ToString());
                    else
                        dictionary.Add(ServerState.RestoreInProcess, _initiatorRestore.IsStart.ToString());
                }

                if (_transferRestore.IsStart)
                {
                    var server = _transferRestore.RemoteServer;
                    if (server != null)
                        dictionary.Add(ServerState.RestoreTransferServer, server.ToString());
                    else
                        dictionary.Add(ServerState.RestoreTransferInProcess, _transferRestore.IsStart.ToString());
                }
                return dictionary;
            }
        }

        public AsyncDbWorkModule(WriterNetModule writerNet, AsyncTaskModule async, DbModuleCollection db,
            RestoreModuleConfiguration initiatorConfiguration,
            RestoreModuleConfiguration transferConfiguration,
            RestoreModuleConfiguration timeoutConfiguration,
            QueueConfiguration queueConfiguration, ServerId local, bool isNeedRestore = false)
        {
            Contract.Requires(initiatorConfiguration != null);
            Contract.Requires(transferConfiguration != null);
            Contract.Requires(timeoutConfiguration != null);
            Contract.Requires(queueConfiguration != null);
            Contract.Requires(db != null);
            Contract.Requires(writerNet != null);
            Contract.Requires(async != null);
            Contract.Requires(local != null);

            _stateHolder = new RestoreStateHolder(isNeedRestore);
            _saver = LoadRestoreStateFromFile();
            _initiatorRestore = new InitiatorRestoreModule(initiatorConfiguration, writerNet, async, _stateHolder,
                _saver);
            _transferRestore = new TransferRestoreModule(transferConfiguration, writerNet, async, 
                db, local, queueConfiguration);
            _timeout = new TimeoutModule(writerNet, async, queueConfiguration,
                db,  timeoutConfiguration);

        }

        private readonly InitiatorRestoreModule _initiatorRestore;
        private readonly TransferRestoreModule _transferRestore;
        private readonly TimeoutModule _timeout;
        
        private List<HashMapRecord> _localHash;

        private RestoreStateHolder _stateHolder;
        private readonly RestoreStateFileLogger _saver;

        public void SetLocalHash(List<HashMapRecord> localHash)
        {
            _localHash = localHash;
        }

        public override void Start()
        {
            _initiatorRestore.Start();
            _transferRestore.Start();
            _timeout.Start();

            if (_saver.IsNeedRestore())
            {
                Task.Delay(Consts.StartRestoreTimeout).ContinueWith(task =>
                {
                    RestoreFromFile(_localHash, _saver.RestoreServers, _saver.TableName);
                });
            }
        }

        public void UpdateModel(List<ServerId> servers)
        {            
            _initiatorRestore.UpdateModel(servers);
            _stateHolder.LocalSendState(true);
            _saver.Save();
        }

        #region Restore process
        
        public void RestoreIncome(ServerId server, bool isSystemUpdated, List<KeyValuePair<string, string>> hash, string tableName, List<HashMapRecord> localMap)
        {
            _transferRestore.RestoreIncome(server, isSystemUpdated, hash, tableName, localMap);
        }

        public void PeriodMessageIncome(ServerId server)
        {
            _initiatorRestore.PeriodMessageIncome(server);
        }
        
        public void LastMessageIncome(ServerId server)
        {            
            _initiatorRestore.LastMessageIncome(server);
        }

        private RestoreStateFileLogger LoadRestoreStateFromFile()
        {
            var saver = new RestoreStateFileLogger(InitInjection.RestoreHelpFile);
            if (!saver.Load())
                return new RestoreStateFileLogger(InitInjection.RestoreHelpFile, _stateHolder);
            
            _stateHolder = saver.StateHolder;
            return saver;
        }

        #endregion

        #region Restore start 

        public void Restore(List<RestoreServer> servers, RestoreState state)
        {
            if (_initiatorRestore.IsStart)
                return;

            _stateHolder.LocalSendState(state);
            _initiatorRestore.Restore(_localHash, servers, _stateHolder.State);
            _saver.Save();
        }

        public void Restore(List<RestoreServer> servers, RestoreState state, string tableName)
        {
            if (_initiatorRestore.IsStart)
                return;

            _stateHolder.LocalSendState(state);
            _initiatorRestore.Restore(_localHash, servers, _stateHolder.State, tableName);
            _saver.Save();
        }

        private void RestoreFromFile(List<HashMapRecord> local, List<RestoreServer> servers, string tableName)
        {
            if (_initiatorRestore.IsStart)
                return;

            _initiatorRestore.RestoreFromFile(local, servers, _stateHolder.State, tableName);
        }       

        #endregion

        public List<ServerId> GetFailedServers()
        {
            return _initiatorRestore.FailedServers;
        }

        public List<RestoreServer> Servers { get { return _initiatorRestore.Servers; } } 

        public ServerId GetRestoreServer()
        {
            return _initiatorRestore.RestoreServer;
        }

        public ServerId GetTransferServer()
        {
            return _transferRestore.RemoteServer;
        }

        public RestoreState DistributorReceive(RestoreState state)
        {
            var old = _stateHolder.State;
            _stateHolder.DistributorSendState(state);
                       
            if (old != _stateHolder.State)
                _saver.Save();
            
            return _stateHolder.State;
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {                
                _transferRestore.Dispose();
                _initiatorRestore.Dispose();
                _timeout.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}
