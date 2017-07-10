using System;
using System.Diagnostics.Contracts;
using System.Threading;
using Ninject;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Interfaces;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class CommonAsyncWorkModule:ControlModule
    {        
        public bool IsStart
        {
            get
            {
                _lock.EnterReadLock();
                bool ret = IsStartNoLock;
                _lock.ExitReadLock();

                return ret;
            }
            protected set
            {
                _lock.EnterWriteLock();
                IsStartNoLock = value;
                _lock.ExitWriteLock();
            }
        }

        protected ReaderWriterLockSlim Lock { get { return _lock; } }

        protected bool IsStartNoLock { get; set; }

        public CommonAsyncWorkModule(StandardKernel kernel)
            :base(kernel)
        {
            IsStartNoLock = false;
            _lock = new ReaderWriterLockSlim();
        }

        public override void Start()
        {
            WriterNet = Kernel.Get<IWriterNetModule>();
            AsyncTaskModule = Kernel.Get<IAsyncTaskModule>();
        }

        protected IWriterNetModule WriterNet;
        protected IAsyncTaskModule AsyncTaskModule;
        private readonly ReaderWriterLockSlim _lock;
    }
}
