using System.Collections.Generic;
using System.Linq;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Processes
{
    internal class SingleServerRestoreProcess : ProcessBase
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public SingleServerRestoreProcess(StandardKernel kernel, IDbModule db, IWriterModel writerModel,
            IWriterNetModule writerNet, string tableName, ServerId remoteServer, bool isSystemUpdated,
            QueueConfiguration queueConfiguration)
            : base(kernel, db, writerModel, writerNet, tableName, isSystemUpdated, queueConfiguration)
        {
            _remoteHashRange = writerModel.GetHashMap(remoteServer)
                .Select(x => new KeyValuePair<string, string>(x.Begin, x.End))
                .ToList();

            _localHashRange = writerModel.GetHashMap(writerModel.Local)
                .Select(x => new KeyValuePair<string, string>(x.Begin, x.End))
                .ToList();

            _remoteServer = remoteServer;
        }

        private readonly List<KeyValuePair<string, string>> _remoteHashRange;
        private readonly List<KeyValuePair<string, string>> _localHashRange;
        private readonly ServerId _remoteServer;

        private void SetRestoreInfo(InnerData data)
        {
            data.Transaction.OperationName = OperationName.RestoreUpdate;
            data.Transaction.OperationType = OperationType.Async;
        }

        #region Package data

        protected override void ProcessDataPackage(List<InnerData> dataList)
        {
            dataList.ForEach(SetRestoreInfo);
            var result = WriterNet.ProcessSync(_remoteServer, dataList);

            ProcessResultPackage(dataList, result as PackageResult);
        }

        private void ProcessResultPackage(List<InnerData> data, RemoteResult result)
        {
            bool send = false;
            do
            {
                while (result is FailNetResult || send)
                {
                    if (_logger.IsDebugEnabled)
                        _logger.DebugFormat("Server {0} unavailable in restore process", _remoteServer);
                    result = WriterNet.ProcessSync(_remoteServer, data);
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
                        Db.Delete(data[i]);
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

        protected override async void ProcessData(InnerData data)
        {
            SetRestoreInfo(data);

            var result = await WriterNet.ProcessAsync(_remoteServer, data);

            ProcessResult(data, result);
        }

        private void ProcessResult(InnerData data, RemoteResult result)
        {
            while (result is FailNetResult)
            {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Server {0} unavailable in restore process", _remoteServer);
                result = WriterNet.ProcessSync(_remoteServer, data);
            }

            ProcessSuccessResult(data);
        }

        private void ProcessSuccessResult(InnerData data)
        {
            PerfCounters.WriterCounters.Instance.RestoreCountSend.Increment();
            PerfCounters.WriterCounters.Instance.RestoreSendPerSec.OperationFinished();

            if (!IsLocalData(data.MetaData))
            {
                Db.Delete(data);
            }
        }

        #endregion

        protected bool IsLocalData(MetaData data)
        {
            return _localHashRange.Exists(
                x =>
                    HashComparer.Compare(x.Key, data.Hash) <= 0 &&
                    HashComparer.Compare(data.Hash, x.Value) <= 0);
        }

        protected override bool IsNeedSendData(MetaData data)
        {
            return _remoteHashRange.Exists(
                x =>
                    HashComparer.Compare(x.Key, data.Hash) <= 0 &&
                    HashComparer.Compare(data.Hash, x.Value) <= 0);
        }
    }
}
