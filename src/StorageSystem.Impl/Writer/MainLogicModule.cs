using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Modules;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.Writer.Interfaces;
using Qoollo.Impl.Writer.PerfCounters;

namespace Qoollo.Impl.Writer
{
    internal class MainLogicModule : ControlModule, IMainLogicModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private IDbModule _db;
        private IDistributorModule _distributor;
        private IWriterModel _model;

        public MainLogicModule(StandardKernel kernel) :base(kernel)
        {
        }

        public override void Start()
        {
            _db = Kernel.Get<IDbModule>();
            _distributor = Kernel.Get<IDistributorModule>();
            _model = Kernel.Get<IWriterModel>();
        }

        #region Process

        public RemoteResult Process(InnerData data)
        {
            _logger.TraceFormat("Process operation = {0}", data.Transaction.OperationName);
            RemoteResult ret = null;
            var local = GetLocal(data);

            switch (data.Transaction.OperationName)
            {
                case OperationName.Create:
                    ret = CheckResult(data, _db.Create(data, local));
                    WriterCounters.Instance.CreatePerSec.OperationFinished();
                    break;
                case OperationName.Custom:
                    ret = CheckResult(data, _db.CustomOperation(data, local));
                    WriterCounters.Instance.CustomOperationPerSec.OperationFinished();
                    break;
                case OperationName.Delete:
                    ret = CheckResult(data, _db.Delete(data));                    
                    break;
                case OperationName.RestoreUpdate:
                    ret = _db.RestoreUpdate(data, local);
                    WriterCounters.Instance.RestoreUpdatePerSec.OperationFinished();
                    WriterCounters.Instance.RestoreCountReceive.Increment();
                    break;
                case OperationName.Update:
                    ret = CheckResult(data, _db.Update(data, local));
                    WriterCounters.Instance.UpdatePerSec.OperationFinished();
                    break;
            }

            return ret;
        }

        public RemoteResult ProcessPackage(List<InnerData> datas)
        {
            RemoteResult ret = null;
            switch (datas[0].Transaction.OperationName)
            {
                case OperationName.RestoreUpdate:
                    ret = _db.RestoreUpdatePackage(datas);
                    WriterCounters.Instance.RestoreUpdatePerSec.OperationFinished();
                    WriterCounters.Instance.RestoreCountReceive.IncrementBy(datas.Count);
                    break;
            }

            return ret;
        }

        public void Rollback(InnerData data)
        {
            _logger.TraceFormat("Rollback operation = {0}", data.Transaction.OperationName);
            var local = GetLocal(data);

            switch (data.Transaction.OperationName)
            {
                case OperationName.Create:
                    CheckResult(data, _db.CreateRollback(data, local));
                    break;
                case OperationName.Custom:
                    CheckResult(data, _db.CustomOperationRollback(data, local));
                    break;
                case OperationName.Delete:
                    CheckResult(data, _db.DeleteRollback(data, local));
                    break;
                case OperationName.Update:
                    CheckResult(data, _db.UpdateRollback(data, local));
                    break;
            }
        }

        public InnerData Read(InnerData data)
        {
            var ret =  _db.ReadExternal(data);
            WriterCounters.Instance.ReadPerSec.OperationFinished();
            return ret;
        }

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {
            SelectSearchResult selectResult;
            var timer = WriterCounters.Instance.QueryAvgTime.StartNew();
            var result = _db.SelectRead(description, out selectResult);
            timer.Complete();

            WriterCounters.Instance.QueryPerSec.OperationFinished();
            return new Tuple<RemoteResult, SelectSearchResult>(result, selectResult);
        }

        private bool GetLocal(InnerData data)
        {
            return  _model.IsMine(data.Transaction.DataHash);
        }

        private RemoteResult CheckResult(InnerData data, RemoteResult result)
        {
            if (result is InnerFailResult)
            {
                if (result.IsError)
                    data.Transaction.SetError();

                data.Transaction.AddErrorDescription(result.Description);
            }
            _distributor.Execute<Transaction, RemoteResult>(data.Transaction);

            if (data.Transaction.OperationType == OperationType.Sync)
                return result;
            
            return null;
        }

        #endregion

    }
}
