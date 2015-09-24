using System;
using System.Collections.Generic;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class RestoreReaderFull:ReaderFullBase
    {
        public Action<InnerData> ProcessData
        {
            get { return _processData; }
        }

        public RestoreReaderFull(Func<MetaData, bool> isMine, Action<InnerData> process, QueueConfiguration queueConfiguration, DbModuleCollection db, bool isBothTables, string tableName, QueueWithParam<InnerData> queue)
            : base(process, queueConfiguration, isBothTables, queue)
        {
            _isMine = isMine;
            _db = db;
            _tableName = tableName;
        }

        private readonly Func<MetaData, bool> _isMine;
        private readonly DbModuleCollection _db;
        private readonly string _tableName;

        private Action<InnerData> _processData;

        protected override SingleReaderBase CreateReader(bool isLocal, int countElements, Action<InnerData> process)
        {
            _processData = process;
            return new RestoreReader(_tableName, isLocal, _isMine, _db, countElements, process);
        }


    }
}
