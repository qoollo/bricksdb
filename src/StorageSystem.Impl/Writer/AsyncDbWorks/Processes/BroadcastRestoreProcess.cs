using System.Collections.Generic;
using System.Linq;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Processes
{
    internal class BroadcastRestoreProcess:ProcessBase
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly List<RestoreServer> _serversToRestore;
        public readonly HashSet<ServerId> FailedServers = new HashSet<ServerId>();

        public BroadcastRestoreProcess(StandardKernel kernel, IDbModule db, IWriterModel writerModel, IWriterNetModule writerNet, List<RestoreServer> serversToRestore, bool isSystemUpdated, bool usePackage)
            : base(kernel, db, writerModel, writerNet, Consts.AllTables, isSystemUpdated, usePackage)
        {
            _serversToRestore = serversToRestore;
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
            var dataListWrap = dataList.Select(d => new InnerDataWrapper(d)).ToList();


            foreach (var server in destination)
            {
                var data = dataListWrap
                    .Where(s => s.Data.MetaData.ServersToSend.Contains(server.Key))
                    .ToList();

                if (data.Count > 0)
                {
                    if (Equals(server.Key, WriterModel.Local))
                    {
                        data.ForEach(d => d.IsLocal = true);
                        data.RemoveAll(d => d.IsLocal);
                    }

                    data.ForEach(d => SetRestoreInfo(d.Data));
                    server.Value.AddRange(data);
                }
            }

            foreach (var serverWithData in destination.Where(s => s.Value.Count != 0))
            {
                var result = WriterNet.ProcessSync(serverWithData.Key,
                    serverWithData.Value.Select(d => d.Data).ToList());

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

            bool isLocalData = false;
            bool isSomeFail = false;

            foreach (var serverId in data.MetaData.ServersToSend)
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
            data.ServersToSend = _serversToRestore.Where(s => s.IsHashInRange(data.Hash)).ToList();

            return data.ServersToSend.Count != 0;
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