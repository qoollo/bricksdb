using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Distributor;
using Qoollo.Impl.Writer.PerfCounters;

namespace Qoollo.Impl.Writer
{
    internal class MainLogicModule:ControlModule
    {
        private DbModule _db;
        private DistributorModule _distributor;        
        private GlobalQueueInner _queue;

        public MainLogicModule(DistributorModule distributor, DbModule db)
        {            
            Contract.Requires( distributor!=null);
            Contract.Requires(db!=null);            

            _db = db;
            _distributor = distributor;
            _queue = GlobalQueue.Queue;
        }

        #region Process

        public RemoteResult Process(InnerData data)
        {
            Logger.Logger.Instance.DebugFormat("Process hash = {0}", data.Transaction.CacheKey);
            RemoteResult ret = null;
            var local = GetLocal(data);

            switch (data.Transaction.OperationName)
            {
                case OperationName.Create:
                    CheckResult(data, _db.Create(data, local));
                    WriterCounters.Instance.CreatePerSec.OperationFinished();
                    break;
                case OperationName.Custom:
                    CheckResult(data, _db.CustomOperation(data, local));
                    WriterCounters.Instance.CustomOperationPerSec.OperationFinished();
                    break;
                case OperationName.Delete:
                    CheckResult(data, _db.Delete(data));
                    WriterCounters.Instance.DeletePerSec.OperationFinished();
                    break;
                case OperationName.RestoreUpdate:
                    ret = _db.RestoreUpdate(data, local);
                    WriterCounters.Instance.RestoreUpdatePerSec.OperationFinished();
                    break;
                case OperationName.Update:
                    CheckResult(data, _db.Update(data, local));
                    WriterCounters.Instance.UpdatePerSec.OperationFinished();
                    break;
            }

            return ret;
        }

        public void Rollback(InnerData data)
        {
            Logger.Logger.Instance.DebugFormat("Rollback hash = {0}", data.Transaction.CacheKey);
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
            var result = _db.SelectRead(description, out selectResult);

            return new Tuple<RemoteResult, SelectSearchResult>(result, selectResult);
        }

        private bool GetLocal(InnerData data)
        {
            return  _distributor.IsMine(data.Transaction.EventHash);
        }

        private void CheckResult(InnerData data, RemoteResult result)
        {
            if (result is InnerFailResult)
            {
                data.Transaction.SetError();
                data.Transaction.AddErrorDescription(result.Description);
            }
            _queue.TransactionAnswerQueue.Add(data.Transaction);
        }

        #endregion

    }
}
