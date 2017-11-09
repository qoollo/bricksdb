using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class RestoreReaderFull<TType>:ReaderFull<TType>
    {
        public Action<TType> ProcessData => ProcessDataWithQueue();

        public RestoreReaderFull(StandardKernel kernel, Func<MetaData, bool> isMine, Action<TType> process,
            int packageSize, IDbModule db, bool isBothTables, string tableName,
            QueueWithParam<TType> queue, bool usePackage)
            : base(kernel, process, packageSize, queue)
        {
            _isMine = isMine;            
            _db = db;
            _isBothTables = isBothTables;
            _tableName = tableName;
            _usePackage = usePackage;
        }

        private readonly Func<MetaData, bool> _isMine;        
        private readonly IDbModule _db;
        private readonly bool _isBothTables;
        private readonly string _tableName;
        private readonly bool _usePackage;

        protected override SingleReaderBase CreateReader(int countElements)
        {
            if (typeof (TType) == typeof (InnerData))
                return new RestoreReader(Kernel, _tableName, _db, new RestoreDataContainer(false, _isBothTables, countElements,
                    ProcessDataWithQueue() as Action<InnerData>, _isMine, _usePackage));

            return new RestoreReader(Kernel, _tableName, _db, new RestoreDataContainer(false, _isBothTables, countElements,
                    ProcessDataWithQueue() as Action<List<InnerData>>, _isMine, _usePackage));
        }
    }
}
