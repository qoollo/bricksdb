using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.DistributorModules.Transaction
{
    internal class TransactionModule : ControlModule
    {
        private TransactionPool _transactionPool;
        private GlobalQueueInner _queue;
        private INetModule _net;
        private QueueConfiguration _queueConfiguration;

        public int CountReplics { get; private set; }

        public TransactionModule(QueueConfiguration configuration, INetModule net, TransactionConfiguration transactionConfiguration,
                                 DistributorHashConfiguration distributorHashConfiguration)
        {
            Contract.Requires(net != null);
            Contract.Requires(transactionConfiguration!=null);
            Contract.Requires(distributorHashConfiguration!=null);
            Contract.Requires(configuration!=null);

            _queueConfiguration = configuration;
            _transactionPool = new TransactionPool(transactionConfiguration.ElementsCount, net,
                                                   distributorHashConfiguration);
            CountReplics = distributorHashConfiguration.CountReplics;
            _net = net;
            _queue = GlobalQueue.Queue;
        }

        #region ControlModule

        public override void Start()
        {
            _queue.DistributorReadQueue.Registrate(_queueConfiguration, ReadProcess);
            _transactionPool.FillPoolUpTo(_transactionPool.MaxElementCount);
        }

        public void ProcessSyncWithExecutor(InnerData data, TransactionExecutor executor)
        {
            if (data.Transaction.OperationName == OperationName.Read)
                Read(data, executor);
            else
                ExecuteTransaction(data, executor);
        }

        private void ExecuteTransaction(InnerData data, TransactionExecutor executor)
        {
            Logger.Logger.Instance.Debug(string.Format("Transaction process data = {0}", data.Transaction.EventHash));

            data.Transaction.StartTransaction();

            executor.Commit(data);
        }

        public TransactionExecutor Rent()
        {
            return _transactionPool.Rent().Element;
        }

        #endregion

        #region Read operation

        private void Read(InnerData data, TransactionExecutor executor)
        {
            if (data.Transaction.IsNeedAllServes)
                ReadLong(data);
            else
                ReadSimple(data, executor);
        }

        private void ReadSimple(InnerData data, TransactionExecutor executor)
        {
            var result = executor.ReadSimple(data);
            ProcessReadResult(data, result, data.Transaction.ProxyServerId);
            data.Transaction.PerfTimer.Complete();
            PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();
        }

        private void ReadLong(InnerData data)
        {
            _queue.DistributorReadQueue.Add(data);
        }

        private void ReadProcess(InnerData data)
        {
            var list = new List<InnerData>();

            foreach (var serverId in data.Transaction.Destination)
            {
                var result = _net.ReadOperation(serverId, data);
                if (result != null)
                    list.Add(result);
            }
            var readResult = ChooseReadResult(list);
            ProcessReadResult(data, readResult, data.Transaction.ProxyServerId);
        }

        private InnerData ChooseReadResult(List<InnerData> list)
        {
            return list.Find(x => x != null && x.Data != null);
        }

        private void ProcessReadResult(InnerData data, InnerData result, ServerId proxyServerId)
        {
            if (result == null)
            {
                result = data;
                result.Data = null;

                result.Transaction.SetError();
                result.Transaction.AddErrorDescription(Errors.ServerIsNotAvailable);
            }

            if(!result.Transaction.IsError)
                result.Transaction.Complete();

            _net.ASendToProxy(proxyServerId, new ReadOperationCompleteCommand(result));
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            _transactionPool.Dispose();
            base.Dispose(isUserCall);
        }
    }
}
