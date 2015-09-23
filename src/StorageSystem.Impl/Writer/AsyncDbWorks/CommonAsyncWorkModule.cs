using System.Diagnostics.Contracts;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class CommonAsyncWorkModule:ControlModule
    {        
        protected WriterNetModule WriterNet;
        protected AsyncTaskModule AsyncTaskModule;
        private bool _isStart;

        public bool IsStart { get { return _isStart; } protected set { _isStart = value; } }

        public CommonAsyncWorkModule(WriterNetModule writerNet, AsyncTaskModule asyncTaskModule)
        {
            Contract.Requires(writerNet!=null);            
            Contract.Requires(asyncTaskModule!=null);
            AsyncTaskModule = asyncTaskModule;
            WriterNet = writerNet;
            _isStart = false;
        }
    }
}
