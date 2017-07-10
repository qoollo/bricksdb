using System;
using System.Diagnostics.Contracts;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class RestoreReader : SingleReaderBase
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly AsyncDbHolder _holder;        
        private readonly string _tableName;        
        private readonly RestoreDataContainer _restoreData;

        public RestoreReader(StandardKernel kernel, string tableName, IDbModule db, RestoreDataContainer restoreData)
            :base(kernel)
        {
            Contract.Requires(db != null);
            Contract.Requires(restoreData != null);

            _tableName = tableName;

            var dbcollection = db as DbModuleCollection;
            _holder = new AsyncDbHolder(dbcollection.GetDbModules);

            _restoreData = restoreData;
            _restoreData.StartNewDb();
        }

        protected override RemoteResult Read()
        {
            PerfCounters.WriterCounters.Instance.RestoreCheckCount.Reset();
            if (_tableName == Consts.AllTables)
                return RestoreAllTables();

            return RestoreSingleDb();
        }

        private RemoteResult RestoreAllTables()
        {
            var db = _holder.GetElement;

            var ret = db.AsyncProcess(_restoreData);
            _restoreData.IsFirstRead = false;            

            if (ret is FailNetResult)
            {
                if(_logger.IsInfoEnabled)
                    _logger.Info("Finish restore table " + db.TableName);

                if (_holder.HasAnother)
                {
                    _holder.Switch();

                    _restoreData.StartNewDb();
                    ret = new SuccessResult();
                }                
            }

            return ret;
        }

        private RemoteResult RestoreSingleDb()
        {            
            var db = GetModule();
            var ret = db.AsyncProcess(_restoreData);
            _restoreData.IsFirstRead = false;
            
            return ret;
        }

        private DbModule GetModule()
        {
            if (_holder.GetElement.TableName == _tableName)
                return _holder.GetElement;

            while (_holder.GetElement.TableName != _tableName)
                _holder.Switch();

            return _holder.GetElement;
        }
    }
}
