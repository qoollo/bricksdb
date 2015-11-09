using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Qoollo.Impl.Common.Server
{
    [Serializable]
    [DataContract]
    public class RestoreServer : ServerId
    {
        [DataMember]        
        public bool IsNeedRestore { get; set; }
        [DataMember]        
        public bool IsRestored { get; set; }
        [DataMember]
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

        public RestoreServer():base("default", -1)
        {
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
