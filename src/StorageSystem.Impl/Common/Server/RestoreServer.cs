using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Common.Server
{
    internal class RestoreServer : ServerId
    {
        public bool IsNeedRestore { get; set; }
        public bool IsRestored { get; set; }
        public bool IsFailed { get; set; }
        public bool IsCurrentServer { get; set; }
        public RestoreServer(string remoteHost, int port)
            : base(remoteHost, port)
        {
            CommonServer();
        }

        public RestoreServer(ServerId server)
            : base(server)
        {
            CommonServer();
        }

        private void CommonServer()
        {
            IsNeedRestore = false;
            IsRestored = false;
            IsFailed = false;
            IsCurrentServer = false;
        }

        public void NeedRestoreInitiate()
        {
            IsNeedRestore = true;
            IsRestored = false;
            IsFailed = false;
        }

        public void AfterFailed()
        {
            IsNeedRestore = true;
            IsRestored = false;
        }

        public bool IsNeedCurrentRestore()
        {
            return IsNeedRestore && !IsRestored;
        }        
    }
}
