using System;
using System.Diagnostics.Contracts;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutReader:SingleReaderBase
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly AsyncDbHolder _holder;
        private readonly RestoreDataContainer _restoreData;        

        public TimeoutReader(StandardKernel kernel, IDbModule db, RestoreDataContainer restoreData)
            :base(kernel)
        {
            Contract.Requires(db != null);
            Contract.Requires(restoreData != null);

            var dbcollection = db as DbModuleCollection;
            _holder = new AsyncDbHolder(dbcollection.GetDbModules);

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
                _logger.Info("Finish delete data in table " + db.TableName);
                
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
