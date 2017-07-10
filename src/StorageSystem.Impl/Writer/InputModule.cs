using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.Writer;
using Qoollo.Impl.Writer.Interfaces;
using Qoollo.Impl.Writer.PerfCounters;

namespace Qoollo.Impl.Writer
{
    internal class InputModule : ControlModule, IRemoteNet, IInputModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly QueueConfiguration _queueConfiguration;
        private IMainLogicModule _mainLogicModule;
        private IGlobalQueue _queue;

        public InputModule(StandardKernel kernel, QueueConfiguration queueConfiguration)
            :base(kernel)
        {
            Contract.Requires(queueConfiguration!=null);
            _queueConfiguration = queueConfiguration;
        }

        public override void Start()
        {
            _mainLogicModule = Kernel.Get<IMainLogicModule>();
            _queue = Kernel.Get<IGlobalQueue>();

            _queue.DbInputRollbackQueue.Registrate(_queueConfiguration, RollbackProcess);
            _queue.DbInputProcessQueue.Registrate(_queueConfiguration, ProcessQueue);
        }

        #region Rollback

        private void RollbackProcess(InnerData data)
        {
            _logger.TraceFormat("Rollback type {0}", data.Transaction.OperationName);

            _mainLogicModule.Rollback(data);
        }        

        public void Rollback(InnerData data)
        {
            //_queue.DbInputRollbackQueue.Add(data);
            RollbackProcess(data);
        }

        #endregion

        #region Process

        public void Process(InnerData data)
        {
            data.Transaction.PerfTimer = WriterCounters.Instance.AverageTimerWithQueue.StartNew();
            _queue.DbInputProcessQueue.Add(data);
            WriterCounters.Instance.IncomePerSec.OperationFinished();
        }

        public RemoteResult ProcessSync(InnerData data)
        {
            return ProcessData(data);
        }

        private RemoteResult ProcessData(InnerData data)
        {
            WriterCounters.Instance.TransactionCount.Increment();
            var timer = WriterCounters.Instance.AverageTimer.StartNew();

            var ret = _mainLogicModule.Process(data);

            timer.Complete();
            data.Transaction.PerfTimer.Complete();

            WriterCounters.Instance.ProcessPerSec.OperationFinished();
            return ret;
        }      

        private void ProcessQueue(InnerData data)
        {            
            ProcessData(data);
        }

        public RemoteResult ProcessSyncPackage(List<InnerData> datas)
        {
            return _mainLogicModule.ProcessPackage(datas);
        }

        #endregion          

        public Task<RemoteResult> ProcessTaskBased(InnerData data)
        {
            throw new NotImplementedException();
        }

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {
            return _mainLogicModule.SelectQuery(description);
        }

        public InnerData ReadOperation(InnerData data)
        {
            WriterCounters.Instance.TransactionCount.Increment();
            WriterCounters.Instance.IncomePerSec.OperationFinished();           

            var read = _mainLogicModule.Read(data);
            
            data.Transaction.PerfTimer.Complete();

            WriterCounters.Instance.ProcessPerSec.OperationFinished();
            return read;
        }
    }
}
