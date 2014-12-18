using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.Server
{
    [DataContract]
    [KnownType(typeof(DistributorDescription))]
    public class ServerId
    {
        [DataMember]
        public int Port { get; private set; }
        [DataMember]
        public string RemoteHost { get; private set; }

        public ServerId(string remoteHost, int port)
        {
            Contract.Requires(port > 0);
            Contract.Requires(remoteHost!="");
            Port = port;
            RemoteHost = remoteHost;
        }

        public ServerId(ServerId server)
        {
            Contract.Requires(server != null);
            Port = server.Port;
            RemoteHost = server.RemoteHost;
        }

        public override int GetHashCode()
        {
            return Port  ^RemoteHost.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            var value = obj as ServerId;
            return RemoteHost == value.RemoteHost && Port == value.Port;
        }

        public override string ToString()
        {
            return RemoteHost+":"+Port;
        }
    }
}
