using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class RestoreProcess : ControlModule
    {
        public bool IsComplete { get { return _reader.IsComplete; } }

        public bool IsQueueEmpty { get { return _reader.IsQueueEmpty; } }        

        public RestoreProcess(List<KeyValuePair<string, string>> remoteHashRange, List<HashMapRecord> localHashRange,
            bool isSystemUpdated, DbModuleCollection db, QueueConfiguration queueConfiguration, string tableName,
            WriterNetModule writerNet, ServerId remote)
        {
            _remoteHashRange = remoteHashRange;
            _db = db;
            _writerNet = writerNet;
            _remote = remote;
            _localHashRange = localHashRange.Select(x => new KeyValuePair<string, string>(x.Begin, x.End)).ToList();
            _reader = new RestoreReaderFull(IsNeedSendData, ProcessData, queueConfiguration, db, isSystemUpdated,
                tableName, GlobalQueue.Queue.DbRestoreQueue, false);
            _reader.Start();
        }

        private readonly List<KeyValuePair<string, string>> _remoteHashRange;
        private readonly DbModuleCollection _db;
        private readonly WriterNetModule _writerNet;
        private readonly ServerId _remote;
        private readonly List<KeyValuePair<string, string>> _localHashRange;
        private readonly RestoreReaderFull _reader;

        public void GetAnotherData()
        {
            _reader.GetAnotherData();
        }

        private async void ProcessData(InnerData data)
        {
            data.Transaction.OperationName = OperationName.RestoreUpdate;
            data.Transaction.OperationType = OperationType.Async;

            var result = await _writerNet.ProcessAsync(_remote, data);

            ProcessResult(data, result);
        }

        private void ProcessResult(InnerData data, RemoteResult result)
        {
            while (result is FailNetResult)
            {
                Logger.Logger.Instance.DebugFormat("Servers {0} unavailable in recover process", _remote);
                result = _writerNet.ProcessSync(_remote, data);
            }

            ProcessSuccessResult(data);
        }

        private void ProcessSuccessResult(InnerData data)
        {
            PerfCounters.WriterCounters.Instance.RestoreCountSend.Increment();
            PerfCounters.WriterCounters.Instance.RestoreSendPerSec.OperationFinished();

            if (!IsLocalData(data.MetaData))
            {
                _db.Delete(data);
            }
        }

        private bool IsLocalData(MetaData data)
        {
            return _localHashRange.Exists(
                x =>
                    HashComparer.Compare(x.Key, data.Hash) <= 0 &&
                    HashComparer.Compare(data.Hash, x.Value) <= 0);
        }

        private bool IsNeedSendData(MetaData data)
        {
            return _remoteHashRange.Exists(
                x =>
                    HashComparer.Compare(x.Key, data.Hash) <= 0 &&
                    HashComparer.Compare(data.Hash, x.Value) <= 0);
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _reader.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }
}
