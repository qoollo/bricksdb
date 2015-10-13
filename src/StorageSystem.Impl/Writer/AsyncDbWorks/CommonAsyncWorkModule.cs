using System;
using System.Diagnostics.Contracts;
using System.Threading;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.WriterNet;

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

        public CommonAsyncWorkModule(WriterNetModule writerNet, AsyncTaskModule asyncTaskModule)
        {
            Contract.Requires(writerNet!=null);            
            Contract.Requires(asyncTaskModule!=null);
            AsyncTaskModule = asyncTaskModule;
            WriterNet = writerNet;
            IsStartNoLock = false;
            _lock = new ReaderWriterLockSlim();
        }

        protected WriterNetModule WriterNet;
        protected AsyncTaskModule AsyncTaskModule;
        private readonly ReaderWriterLockSlim _lock;
    }
}
