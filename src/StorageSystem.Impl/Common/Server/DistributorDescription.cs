using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.Server
{    
    [DataContract]
    internal class DistributorDescription:ServerId
    {
        [DataMember]
        public int Load { get; private set; }
        [DataMember]
        public string Hash { get; private set; }
        [DataMember]
        public bool IsAvailable { get; private set; }

        public DistributorDescription(string hash, ServerId server):base(server)
        {
            Hash = hash;
            IsAvailable = true;
        }

        public void UpdateLoad(int load)
        {
            lock (this)
            {
                Load = load;                
            }
        }

        public void NotAvailable()
        {
            IsAvailable = false;
        }

        public void Available()
        {
            IsAvailable = true;
        }
    }
}
