using System;
using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutReaderFull:ReaderFull<InnerData>
    {
        private readonly Func<MetaData, bool> _isMine;        
        private readonly DbModuleCollection _db;

        public TimeoutReaderFull(StandardKernel kernel, Func<MetaData, bool> isMine, Action<InnerData> process,
            QueueConfiguration queueConfiguration, DbModuleCollection db, QueueWithParam<InnerData> queue)
            : base(kernel, process, queueConfiguration, queue)
        {
            _isMine = isMine;            
            _db = db;
        }

        protected override SingleReaderBase CreateReader(int countElements)
        {
            return new TimeoutReader(Kernel, _db,
                new RestoreDataContainer(true, true, countElements, ProcessDataWithQueue(), _isMine, false));
        }
    }
}
