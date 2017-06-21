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
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.TestSupport;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class RestoreProcess : ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public bool IsComplete => _reader.IsComplete;

        public bool IsQueueEmpty => _reader.IsQueueEmpty;

        public RestoreProcess(List<KeyValuePair<string, string>> remoteHashRange, List<HashMapRecord> localHashRange,
            bool isSystemUpdated, DbModuleCollection db, QueueConfiguration queueConfiguration, string tableName,
            WriterNetModule writerNet, ServerId remote)
        {
            _remoteHashRange = remoteHashRange;
            _db = db;
            _writerNet = writerNet;
            _remote = remote;
            _localHashRange = localHashRange.Select(x => new KeyValuePair<string, string>(x.Begin, x.End)).ToList();

            if (InitInjection.RestoreUsePackage)
                _reader = new RestoreReaderFull<List<InnerData>>(IsNeedSendData, ProcessDataPackage,
                        queueConfiguration, db, isSystemUpdated, tableName, GlobalQueue.Queue.DbRestorePackageQueue,
                        true);
            else
                _reader = new RestoreReaderFull<InnerData>(IsNeedSendData, ProcessData, queueConfiguration, db,
                    isSystemUpdated, tableName, GlobalQueue.Queue.DbRestoreQueue, false);

            _reader.Start();
        }

        private readonly List<KeyValuePair<string, string>> _remoteHashRange;
        private readonly DbModuleCollection _db;
        private readonly WriterNetModule _writerNet;
        private readonly ServerId _remote;
        private readonly List<KeyValuePair<string, string>> _localHashRange;
        private readonly ReaderFullBase _reader;

        public void GetAnotherData()
        {
            _reader.GetAnotherData();
        }

        private void SetRestoreInfo(InnerData data)
        {
            data.Transaction.OperationName = OperationName.RestoreUpdate;
            data.Transaction.OperationType = OperationType.Async;
        }

        #region Package data

        private void ProcessDataPackage(List<InnerData> data)
        {
            data.ForEach(SetRestoreInfo);
            var result = _writerNet.ProcessSync(_remote, data);

            ProcessResultPackage(data, result as PackageResult);
        }

        private void ProcessResultPackage(List<InnerData> data, RemoteResult result)
        {
            bool send = false;
            do
            {
                while (result is FailNetResult || send)
                {
                    if (_logger.IsDebugEnabled)
                        _logger.DebugFormat("Servers {0} unavailable in recover process", _remote);
                    result = _writerNet.ProcessSync(_remote, data);
                    send = false;
                }
                data = ProcessSuccessResult(data, result as PackageResult);
                send = true;
            } while (data.Count != 0);
        }

        private List<InnerData> ProcessSuccessResult(List<InnerData> data, PackageResult resultList)
        {
            var results = resultList.Result;
            var fail = new List<InnerData>();
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                {                    
                    PerfCounters.WriterCounters.Instance.RestoreSendPerSec.OperationFinished();

                    if (!IsLocalData(data[i].MetaData))
                    {
                        _db.Delete(data[i]);
                    }
                }
                else
                    fail.Add(data[i]);
            }
            PerfCounters.WriterCounters.Instance.RestoreCountSend.IncrementBy(data.Count - fail.Count);
            return fail;
        }

        #endregion

        #region Single data

        private async void ProcessData(InnerData data)
        {
            SetRestoreInfo(data);

            var result = await _writerNet.ProcessAsync(_remote, data);

            ProcessResult(data, result);
        }        

        private void ProcessResult(InnerData data, RemoteResult result)
        {
            while (result is FailNetResult)
            {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Servers {0} unavailable in recover process", _remote);
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

        #endregion

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
