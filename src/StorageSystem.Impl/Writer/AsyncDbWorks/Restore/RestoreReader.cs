using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
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

        public RestoreReader(string tableName, bool local, Func<MetaData, bool> isMine, DbModuleCollection db,
            int countElements, Action<InnerData> process)
        {
            Contract.Requires(db != null);
            Contract.Requires(process != null);
            Contract.Requires(countElements > 0);
            Contract.Requires(isMine != null);

            _tableName = tableName;
            _holder = new AsyncDbHolder(db.GetDbModules);

            _restoreData = new RestoreDataContainer(false, local, countElements, process, isMine, true);
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
