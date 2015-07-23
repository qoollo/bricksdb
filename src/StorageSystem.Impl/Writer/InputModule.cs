using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Timestamps;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.Writer;
using Qoollo.Impl.Writer.PerfCounters;

namespace Qoollo.Impl.Writer
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

        #region Rollback

        private void RollbackProcess(InnerData data)
        {
            Logger.Logger.Instance.DebugFormat("Rollback type {0}, hash {1}", data.Transaction.OperationName,
                data.Transaction.DataHash);
            data.Transaction.MakeStamp("writer start rollback data");

            _mainLogicModule.Rollback(data);
        }

        public void Rollback(InnerData data)
        {
            data.Transaction.Start("writer");
            data.Transaction.MakeStamp("writer start add rollback queue");

            _queue.DbInputRollbackQueue.Add(data);
        }

        #endregion

        #region Process

        private RemoteResult ProcessData(InnerData data)
        {
            data.Transaction.MakeStamp("writer start process data");
            WriterCounters.Instance.TransactionCount.Increment();
            Logger.Logger.Instance.DebugFormat("Create hash {0}", data.Transaction.DataHash);
            var timer = WriterCounters.Instance.AverageTimer.StartNew();

            var ret = _mainLogicModule.Process(data);

            timer.Complete();
            data.Transaction.PerfTimer.Complete();
            data.Transaction.MakeStamp("writer finish process data");

            WriterCounters.Instance.ProcessPerSec.OperationFinished();
            return ret;
        }      

        public void Process(InnerData data)
        {            
            data.Transaction.Start("writer");
            data.Transaction.MakeStamp("writer add to process queue");
            data.Transaction.PerfTimer = WriterCounters.Instance.AverageTimerWithQueue.StartNew();

            _queue.DbInputProcessQueue.Add(data);
            WriterCounters.Instance.IncomePerSec.OperationFinished();
        }        

        private void ProcessInner(InnerData data)
        {
            ProcessData(data);
        }

        public RemoteResult ProcessSync(InnerData data)
        {
            data.Transaction.StartTransaction();
            return ProcessData(data);
        }

        #endregion

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {
            return _mainLogicModule.SelectQuery(description);
        }

        public InnerData ReadOperation(InnerData data)
        {
            WriterCounters.Instance.TransactionCount.Increment();
            WriterCounters.Instance.IncomePerSec.OperationFinished();
            var timer = WriterCounters.Instance.AverageTimer.StartNew();
            data.Transaction.MakeStamp("writer start read data");

            var read = _mainLogicModule.Read(data);

            timer.Complete();
            data.Transaction.PerfTimer.Complete();
            data.Transaction.MakeStamp("writer finish read data");

            WriterCounters.Instance.ProcessPerSec.OperationFinished();
            return read;
        }
    }
}
