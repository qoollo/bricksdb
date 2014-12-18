using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.DbController;

namespace Qoollo.Impl.DbController
{
    internal class InputModule:ControlModule,IRemoteNet
    {
        private readonly QueueConfiguration _queueConfiguration;
        private readonly MainLogicModule _mainLogicModule;
        private readonly GlobalQueueInner _queue;

        public InputModule(MainLogicModule mainLogic, QueueConfiguration queueConfiguration)
        {
            Contract.Requires(queueConfiguration!=null);
            Contract.Requires(mainLogic!=null);
            _queueConfiguration = queueConfiguration;
            _mainLogicModule = mainLogic;
            _queue = GlobalQueue.Queue;
        }

        public override void Start()
        {
            _queue.DbInputRollbackQueue.Registrate(_queueConfiguration, RollbackProcess);
            _queue.DbInputProcessQueue.Registrate(_queueConfiguration, ProcessInner);
        }

        #region Execute

        private void RollbackProcess(InnerData data)
        {
            Logger.Logger.Instance.DebugFormat("Rollback type {0}, hash {1}", data.Transaction.OperationName, data.Transaction.EventHash);

            _mainLogicModule.Rollback(data);
        }

        private RemoteResult ProcessData(InnerData data)
        {
            PerfCounters.DbControllerCounters.Instance.TransactionCount.Increment();
            Logger.Logger.Instance.DebugFormat("Create hash {0}", data.Transaction.EventHash);
            var timer = PerfCounters.DbControllerCounters.Instance.AverageTimer.StartNew();

            var ret = _mainLogicModule.Process(data);

            timer.Complete();
            data.Transaction.PerfTimer.Complete();

            PerfCounters.DbControllerCounters.Instance.ProcessPerSec.OperationFinished();
            return ret;
        }

        #endregion        

        #region Insert into queue

        public void Process(InnerData data)
        {
            data.Transaction.PerfTimer = PerfCounters.DbControllerCounters.Instance.AverageTimerWithQueue.StartNew();
            _queue.DbInputProcessQueue.Add(data);
            PerfCounters.DbControllerCounters.Instance.IncomePerSec.OperationFinished();
        }

        public void Rollback(InnerData data)
        {
            _queue.DbInputRollbackQueue.Add(data);
        }

        #endregion

        #region Sync

        private void ProcessInner(InnerData data)
        {
            ProcessData(data);
        }                

        public RemoteResult ProcessSync(InnerData data)
        {
            return ProcessData(data);
        }

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {            
            return _mainLogicModule.SelectQuery(description);
        }

        #endregion

        public InnerData ReadOperation(InnerData data)
        {
            PerfCounters.DbControllerCounters.Instance.TransactionCount.Increment();
            PerfCounters.DbControllerCounters.Instance.IncomePerSec.OperationFinished();
            var timer = PerfCounters.DbControllerCounters.Instance.AverageTimer.StartNew();

            var read = _mainLogicModule.Read(data);

            timer.Complete();
            data.Transaction.PerfTimer.Complete();

            PerfCounters.DbControllerCounters.Instance.ProcessPerSec.OperationFinished();
            return read;
        }
    }
}
