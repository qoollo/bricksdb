using System;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutReaderFull:ReaderFullBase
    {
        private Func<MetaData, bool> _isMine;
        private DbModuleCollection _db;

        public TimeoutReaderFull(Func<MetaData, bool> isMine, Action<InnerData> process,
            QueueConfiguration queueConfiguration,
            DbModuleCollection db, bool isBothTables, QueueWithParam<InnerData> queue)
            : base(process, queueConfiguration, isBothTables, queue)
        {
            _isMine = isMine;
            _db = db;
        }

        protected override SingleReaderBase CreateReader(bool isLocal, int countElements, Action<InnerData> process)
        {
            return new TimeoutReader(_isMine, _db, countElements, process);
        }
    }
}
