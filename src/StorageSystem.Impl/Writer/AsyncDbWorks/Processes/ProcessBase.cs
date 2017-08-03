using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.AsyncDbWorks.Readers;
using Qoollo.Impl.Writer.AsyncDbWorks.Restore;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Processes
{
    internal abstract class ProcessBase:ControlModule
    {
        public bool IsComplete => _reader.IsComplete;

        public bool IsQueueEmpty => _reader.IsQueueEmpty;

        protected IWriterModel WriterModel { get; }
        protected IWriterNetModule WriterNet { get; }
        protected IDbModule Db { get; }

        private readonly ReaderFullBase _reader;

        protected ProcessBase(StandardKernel kernel, IDbModule db, IWriterModel writerModel,
            IWriterNetModule writerNet, string tableName, bool isSystemUpdated, bool usePackage)
            : base(kernel)
        {
            Db = db;
            WriterModel = writerModel;
            WriterNet = writerNet;

            var queue = kernel.Get<IGlobalQueue>();
            var config = kernel.Get<IWriterConfiguration>();

            if (usePackage)
                _reader = new RestoreReaderFull<List<InnerData>>(kernel, IsNeedSendData, ProcessDataPackage,
                    config.PackageSizeRestore, db, isSystemUpdated, tableName, queue.DbRestorePackageQueue, true);
            else
                _reader = new RestoreReaderFull<InnerData>(kernel, IsNeedSendData, ProcessData,
                    config.PackageSizeRestore, db, isSystemUpdated, tableName, queue.DbRestoreQueue, false);
        }

        public override void Start()
        {
            _reader.Start();
        }

        public void GetAnotherData()
        {
            _reader.GetAnotherData();
        }

        protected abstract void ProcessDataPackage(List<InnerData> obj);

        protected abstract void ProcessData(InnerData data);

        protected abstract bool IsNeedSendData(MetaData data);

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _reader.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }
}