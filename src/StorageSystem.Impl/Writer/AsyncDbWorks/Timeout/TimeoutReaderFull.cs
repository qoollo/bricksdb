using System;
using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutReaderFull:ReaderFull<InnerData>
    {
        private readonly Func<MetaData, bool> _isMine;        
        private readonly IDbModule _db;

        public TimeoutReaderFull(StandardKernel kernel, Func<MetaData, bool> isMine, Action<InnerData> process,
            int packageSize, IDbModule db, QueueWithParam<InnerData> queue)
            : base(kernel, process, packageSize, queue)
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
