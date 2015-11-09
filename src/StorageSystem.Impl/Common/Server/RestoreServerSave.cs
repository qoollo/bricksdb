using System;
using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.Server
{
    [Serializable]
    [DataContract]
    public class RestoreServerSave
    {
        [DataMember]
        public bool IsNeedRestore { get; set; }
        [DataMember]
        public bool IsRestored { get; set; }
        [DataMember]
        public bool IsFailed { get; set; }
        [DataMember]
        public int Port { get; set; }
        [DataMember]
        public string Host { get; set; }

        public RestoreServerSave(RestoreServer server)
        {
            IsNeedRestore = server.IsNeedRestore;
            IsRestored = server.IsRestored;
            IsFailed = server.IsFailed;
            Port = server.Port;
            Host = server.RemoteHost;
        }

        public RestoreServerSave()
        {
        }

        public RestoreServer Convert()
        {
            return new RestoreServer(Host, Port)
            {
                IsNeedRestore = IsNeedRestore,
                IsRestored = IsRestored,
                IsFailed = IsFailed
            };
        }
    }
}