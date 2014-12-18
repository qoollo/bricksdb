using System;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DbController.AsyncDbWorks.Readers;
using Qoollo.Impl.DbController.Db;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.DbController.AsyncDbWorks.Restore
{
    internal class RestoreReaderFull:ReaderFullBase
    {
        private Func<MetaData, bool> _isMine;
        private DbModuleCollection _db;
        private string _tableName;

        public RestoreReaderFull(Func<MetaData, bool> isMine, Action<InnerData> process, QueueConfiguration queueConfiguration,
            DbModuleCollection db, bool isBothTables, string tableName, QueueWithParam<InnerData> queue)
            : base(process, queueConfiguration, isBothTables, queue)
        {
            _isMine = isMine;
            _db = db;
            _tableName = tableName;
        }

        protected override SingleReaderBase CreateReader(bool isLocal, int countElements, Action<InnerData> process)
        {
            return new RestoreReader(_tableName, isLocal, _isMine, _db, countElements, process);
        }
    }
}
