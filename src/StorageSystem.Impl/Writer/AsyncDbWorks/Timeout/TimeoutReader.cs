using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutReader:SingleReaderBase
    {        
        private readonly AsyncDbHolder _holder;
        private readonly RestoreDataContainer _restoreData;        

        public TimeoutReader(DbModuleCollection db, RestoreDataContainer restoreData)
        {
            Contract.Requires(db != null);
            Contract.Requires(restoreData != null);            
            _holder = new AsyncDbHolder(db.GetDbModules);

            _restoreData = restoreData;
            _restoreData.StartNewDb();
        }

        protected override RemoteResult Read()
        {
            PerfCounters.WriterCounters.Instance.RestoreCheckCount.Reset();
            GetAnotherData();

            return RestoreAllTables();
        }        

        private RemoteResult RestoreAllTables()
        {
            var db = _holder.GetElement;

            var ret = db.AsyncProcess(_restoreData);
            _restoreData.IsFirstRead = false;            

            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.Info("Finish delete data in table " + db.TableName);
                
                if (_holder.HasAnother)
                {
                    _holder.Switch();

                    _restoreData.StartNewDb();
                    ret = new SuccessResult();
                }        
            }

            return ret;
        }
    }
}
