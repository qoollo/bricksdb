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

            foreach (var data in dataList)
            {
                SetRestoreInfo(data);
                var destinationServers = WriterModel.GetDestination(data.MetaData.Hash);
                foreach (var serverId in destinationServers)
                {
                    destination[serverId].Add(data);
                }
            }

            var failData = new HashSet<InnerData>();

            foreach (var serverWithData in destination)
            {
                var result = WriterNet.ProcessSync(serverWithData.Key, serverWithData.Value);

                var data = new List<InnerData>();

                if (result is PackageResult)
                    data = ProcessSuccessResult(serverWithData.Value, result as PackageResult);
                else
                    data = ProcessFailPackage(serverWithData.Key, serverWithData.Value);

                data.ForEach(d => failData.Add(d));
                TryAddFailedServer(serverWithData.Key);
            }

            //todo delete non local data
        }

        private List<InnerData> ProcessFailPackage(ServerId server, List<InnerData> data)
        {
            int counter = 0;
            RemoteResult result;
            do
            {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Server {0} unavailable in restore process", server);
                result = WriterNet.ProcessSync(server, data);
            } while (result is FailNetResult && counter++ < 3);

            if (result is PackageResult)
                data = ProcessSuccessResult(data, result as PackageResult);

            return data;
        }

        private List<InnerData> ProcessSuccessResult(List<InnerData> data, PackageResult resultList)
        {
            var results = resultList.Result;
            var fail = new List<InnerData>();
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                    PerfCounters.WriterCounters.Instance.RestoreSendPerSec.OperationFinished();
                else
                    fail.Add(data[i]);
            }
            PerfCounters.WriterCounters.Instance.RestoreCountSend.IncrementBy(data.Count - fail.Count);
            return fail;
        }

        private Dictionary<ServerId, List<InnerData>> GetDestinationCollection()
        {
            var destination = new Dictionary<ServerId, List<InnerData>>();
            WriterModel.Servers.ForEach(s => destination.Add(s, new List<InnerData>()));
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
    }
}