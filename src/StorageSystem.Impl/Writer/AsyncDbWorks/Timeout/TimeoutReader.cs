using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutReader:SingleReaderBase
    {
        private DbModuleCollection _db;
        private Action<InnerData> _process;
        private int _countElements;
        private Func<MetaData, bool> _isMine;

        private bool _isFirstRead;
        private object _lastId;
        private AsyncDbHolder _holder;

        public TimeoutReader(Func<MetaData, bool> isMine, DbModuleCollection db, int countElements, Action<InnerData> process)
        {
            Contract.Requires(db != null);
            Contract.Requires(process != null);
            Contract.Requires(countElements > 0);
            Contract.Requires(isMine != null);

            _isMine = isMine;
            _countElements = countElements;
            _process = process;
            _db = db;

            _holder = new AsyncDbHolder(db.GetDbModules);

            StartNewDb();
        }

        protected override RemoteResult Read()
        {
            GetAnotherData();

            return RestoreAllTables();
        }

        private void StartNewDb()
        {
            _isFirstRead = true;
            _lastId = null;
        }

        private RemoteResult RestoreAllTables()
        {
            var db = _holder.GetElement;

            var ret = db.AsyncProcess(true, true, _countElements, _process, _isMine, _isFirstRead, ref _lastId);
            _isFirstRead = false;

            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.Info("Finish delete data in table " + db.TableName);
                
                if (_holder.HasAnother)
                {
                    _holder.Switch();

                    StartNewDb();
                    ret = new SuccessResult();
                }        
            }

            return ret;
        }
    }
}
