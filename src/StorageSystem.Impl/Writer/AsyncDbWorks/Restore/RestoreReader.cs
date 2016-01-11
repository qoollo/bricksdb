using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class RestoreReader : SingleReaderBase
    {
        private readonly AsyncDbHolder _holder;        
        private readonly string _tableName;        
        private readonly RestoreDataContainer _restoreData;

        public RestoreReader(string tableName, DbModuleCollection db, RestoreDataContainer restoreData)
        {
            Contract.Requires(db != null);
            Contract.Requires(restoreData != null);

            _tableName = tableName;            
            _holder = new AsyncDbHolder(db.GetDbModules);

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
                Logger.Logger.Instance.Info("Finish restore table " + db.TableName);

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
