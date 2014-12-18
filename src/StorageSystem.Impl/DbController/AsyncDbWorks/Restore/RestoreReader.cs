using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.DbController.AsyncDbWorks.Readers;
using Qoollo.Impl.DbController.AsyncDbWorks.Support;
using Qoollo.Impl.DbController.Db;

namespace Qoollo.Impl.DbController.AsyncDbWorks.Restore
{
    internal class RestoreReader : SingleReaderBase
    {
        private AsyncDbHolder _holder;
        private Action<InnerData> _process;
        private int _countElements;
        private string _tableName;
        private Func<MetaData, bool> _isMine;
        private bool _local;

        private bool _isFirstRead;
        private object _lastId;

        public RestoreReader(string tableName, bool local, Func<MetaData, bool> isMine, DbModuleCollection db,
            int countElements, Action<InnerData> process)
        {
            Contract.Requires(db != null);
            Contract.Requires(process != null);
            Contract.Requires(countElements > 0);
            Contract.Requires(isMine != null);

            _tableName = tableName;
            _isMine = isMine;
            _countElements = countElements;
            _process = process;
            _holder = new AsyncDbHolder(db.GetDbModules);
            _local = local;

            StartNewDb();
        }

        protected override RemoteResult Read()
        {
            if (_tableName == Consts.AllTables)
                return RestoreAllTables();

            return RestoreSingleDb();
        }

        private void StartNewDb()
        {
            _isFirstRead = true;
            _lastId = null;
        }

        private RemoteResult RestoreAllTables()
        {
            var db = _holder.GetElement;

            var ret = db.AsyncProcess(false, _local, _countElements, _process, _isMine, _isFirstRead, ref _lastId);
            _isFirstRead = false;

            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.Info("Finish restore table " + db.TableName);

                if (_holder.HasAnother)
                {
                    _holder.Switch();

                    StartNewDb();
                    ret = new SuccessResult();
                }                
            }

            return ret;
        }

        private RemoteResult RestoreSingleDb()
        {
            var db = GetModule();

            var ret = db.AsyncProcess(false, _local, _countElements, _process, _isMine, _isFirstRead, ref _lastId);
            _isFirstRead = false;
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
