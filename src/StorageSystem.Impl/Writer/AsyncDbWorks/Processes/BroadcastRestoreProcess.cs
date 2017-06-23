using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Processes
{
    internal class BroadcastRestoreProcess:ProcessBase
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public readonly HashSet<ServerId> FailedServers = new HashSet<ServerId>();
         
        public BroadcastRestoreProcess(DbModuleCollection db, WriterModel writerModel, WriterNetModule writerNet,
            bool isSystemUpdated, QueueConfiguration queueConfiguration)
            : base(db, writerModel, writerNet, Consts.AllTables, isSystemUpdated, queueConfiguration)
        {
        }

        private void SetRestoreInfo(InnerData data)
        {
            data.Transaction.OperationName = OperationName.RestoreUpdate;
            data.Transaction.OperationType = OperationType.Async;
        }

        #region Package

        protected override void ProcessDataPackage(List<InnerData> dataList)
        {
            var destination = GetDestinationCollection();
            var dataListWrap = dataList.Select(d => new InnerDataWrapper(d));

            foreach (var data in dataListWrap)
            {
                SetRestoreInfo(data.Data);
                var destinationServers = WriterModel.GetDestination(data.Hash);
                foreach (var serverId in destinationServers)
                {
                    destination[serverId].Add(data);
                }
            }

            foreach (var serverWithData in destination)
            {
                var result = WriterNet.ProcessSync(serverWithData.Key, serverWithData.Value.Select(d => d.Data).ToList());

                bool isSomeFail;

                if (result is PackageResult)
                    isSomeFail = ProcessSuccessResult(serverWithData.Value, result as PackageResult);
                else
                    isSomeFail = ProcessFailPackage(serverWithData.Key, serverWithData.Value);

                if (isSomeFail)
                    TryAddFailedServer(serverWithData.Key);
            }

            dataListWrap
                .Where(d => d.CanDelete())
                .ToList()
                .ForEach(d => Db.Delete(d.Data));
        }

        private bool ProcessFailPackage(ServerId server, List<InnerDataWrapper> data)
        {
            int counter = 0;
            var dataToSend = data.Select(d => d.Data).ToList();

            RemoteResult result;
            do
            {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Server {0} unavailable in restore process", server);
                result = WriterNet.ProcessSync(server, dataToSend);
            } while (result is FailNetResult && counter++ < 3);

            if (result is PackageResult)
                return ProcessSuccessResult(data, result as PackageResult);

            data.ForEach(d => d.IsSomeFail = true);
            return true;
        }

        private bool ProcessSuccessResult(List<InnerDataWrapper> data, PackageResult resultList)
        {
            var results = resultList.Result;
            int countFail = 0;
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                    PerfCounters.WriterCounters.Instance.RestoreSendPerSec.OperationFinished();
                else
                {
                    data[i].IsSomeFail = true;
                    countFail ++;
                }
            }
            PerfCounters.WriterCounters.Instance.RestoreCountSend.IncrementBy(data.Count - countFail);

            return countFail != 0;
        }

        private Dictionary<ServerId, List<InnerDataWrapper>> GetDestinationCollection()
        {
            var destination = new Dictionary<ServerId, List<InnerDataWrapper>>();
            WriterModel.Servers.ForEach(s => destination.Add(s, new List<InnerDataWrapper>()));
            return destination;
        }

        #endregion

        #region Single dataList

        protected override async void ProcessData(InnerData data)
        {
            SetRestoreInfo(data);
            var destinationServers = WriterModel.GetDestination(data.MetaData.Hash);

            bool isLocalData = false;
            bool isSomeFail = false;

            foreach (var serverId in destinationServers)
            {
                if (Equals(serverId, WriterModel.Local))
                {
                    isLocalData = true;
                }

                if (isLocalData && data.MetaData.IsLocal)
                {
                    continue;
                }

                var result = await WriterNet.ProcessAsync(serverId, data);
                if (result.IsError && ProcessFailResult(data, serverId))
                {
                    isSomeFail = true;
                    TryAddFailedServer(serverId);
                }
            }

            PerfCounters.WriterCounters.Instance.RestoreCountSend.Increment();
            PerfCounters.WriterCounters.Instance.RestoreSendPerSec.OperationFinished();

            if (!isLocalData && !isSomeFail)
            {
                Db.Delete(data);
            }
        }

        private bool ProcessFailResult(InnerData data, ServerId server)
        {
            int counter = 0;
            RemoteResult result;
            do
            {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Server {0} unavailable in restore process", server);
                result = WriterNet.ProcessSync(server, data);
            } while (result is FailNetResult && counter ++<3);

            return result.IsError;
        }

        #endregion

        private void TryAddFailedServer(ServerId server)
        {
            FailedServers.Add(server);
        }

        protected override bool IsNeedSendData(MetaData data)
        {
            return true;
        }

        internal class InnerDataWrapper
        {
            public InnerDataWrapper(InnerData data)
            {
                Data = data;
                IsLocal = false;
                IsSomeFail = false;
            }

            public InnerData Data { get; }
            public bool IsLocal { get; set; }
            public bool IsSomeFail { get; set; }

            public string Hash => Data.MetaData.Hash;

            public bool CanDelete()
            {
                return !IsLocal && !IsSomeFail;
            }
        }
    }
}