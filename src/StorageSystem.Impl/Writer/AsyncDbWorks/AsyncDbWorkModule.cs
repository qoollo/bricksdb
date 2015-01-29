using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.AsyncDbWorks.Restore;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Timeout;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class AsyncDbWorkModule:ControlModule
    {
        private InitiatorRestoreModule _initiatorRestore;
        private TransferRestoreModule _transfer;
        private TimeoutModule _timeout;

        private RestoreStateHelper _stateHelper;
        public bool IsNeedRestore
        {
            get
            {
                _stateHelper.InitiatorState(_initiatorRestore.IsStart);

                return _stateHelper.IsNeedRestore;
            }
        }

        public bool IsStarted
        {
            get
            {
                return _initiatorRestore.IsStart;
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

            _stateHelper = new RestoreStateHelper(isNeedRestore);

            _initiatorRestore = new InitiatorRestoreModule(initiatorConfiguration, writerNet, async);
            _transfer = new TransferRestoreModule(transferConfiguration, writerNet, async, 
                db, local, queueConfiguration);
            _timeout = new TimeoutModule(writerNet, async, queueConfiguration,
                db,  timeoutConfiguration);
        }

        public override void Start()
        {
            _initiatorRestore.Start();
            _transfer.Start();
            _timeout.Start();
        }
        
        public void Restore(List<HashMapRecord> local, List<ServerId> servers, bool isModelUpdated)
        {
            if (_initiatorRestore.IsStart)
                return;

            bool ret =  _initiatorRestore.Restore(local,servers, isModelUpdated);

            if(ret)
                _stateHelper.RestoreStart();
        }

        public void Restore(List<HashMapRecord> local, List<ServerId> servers, bool isModelUpdated, string tableName)
        {
            if (_initiatorRestore.IsStart)
                return;

            bool ret = _initiatorRestore.Restore(local, servers, isModelUpdated, tableName);

            if (ret)
                _stateHelper.RestoreStart();
        }

        public void RestoreIncome(ServerId server, bool isSystemUpdated, List<KeyValuePair<string, string>> hash,
            string tableName)
        {
            _transfer.RestoreIncome(server, isSystemUpdated, hash, tableName);
        }

        public void PeriodMessageIncome(ServerId server)
        {
            _initiatorRestore.PeriodMessageIncome(server);
        }
        
        public void LastMessageIncome(ServerId server)
        {            
            _initiatorRestore.LastMessageIncome(server);
        }

        public bool IsRestoreComplete()
        {
            return !_initiatorRestore.IsStart;
        }

        public List<ServerId> GetFailedServers()
        {
            return _initiatorRestore.FailedServers;
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {                
                _transfer.Dispose();
                _initiatorRestore.Dispose();
                _timeout.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}
