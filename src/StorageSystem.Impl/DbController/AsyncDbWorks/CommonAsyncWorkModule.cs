using System.Diagnostics.Contracts;
using Qoollo.Impl.DbController.DbControllerNet;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;

namespace Qoollo.Impl.DbController.AsyncDbWorks
{
    internal class CommonAsyncWorkModule:ControlModule
    {        
        protected DbControllerNetModule DbControllerNet;
        protected AsyncTaskModule _asyncTaskModule;
        protected bool _isStart;

        public bool IsStart { get { return _isStart; } }

        public CommonAsyncWorkModule(DbControllerNetModule dbControllerNet, AsyncTaskModule asyncTaskModule)
        {
            Contract.Requires(dbControllerNet!=null);            
            Contract.Requires(asyncTaskModule!=null);
            _asyncTaskModule = asyncTaskModule;
            DbControllerNet = dbControllerNet;
            _isStart = false;
        }
    }
}
