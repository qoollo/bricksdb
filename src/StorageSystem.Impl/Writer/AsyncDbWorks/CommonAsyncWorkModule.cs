using System.Diagnostics.Contracts;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class CommonAsyncWorkModule:ControlModule
    {        
        protected WriterNetModule WriterNet;
        protected AsyncTaskModule _asyncTaskModule;
        protected bool _isStart;

        public bool IsStart { get { return _isStart; } }

        public CommonAsyncWorkModule(WriterNetModule writerNet, AsyncTaskModule asyncTaskModule)
        {
            Contract.Requires(writerNet!=null);            
            Contract.Requires(asyncTaskModule!=null);
            _asyncTaskModule = asyncTaskModule;
            WriterNet = writerNet;
            _isStart = false;
        }
    }
}
