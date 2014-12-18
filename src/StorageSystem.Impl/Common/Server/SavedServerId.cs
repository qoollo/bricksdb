using System;
using System.Xml.Serialization;
using Qoollo.Impl.Common.HashFile;

namespace Qoollo.Impl.Common.Server
{
    [Serializable]
    public class SavedServerId
    {
        [XmlAttribute("Localhost")]
        public string Host { get; set; }

        [XmlAttribute("PortForDistributor")]
        public int PortForDistributor { get; set; }

        [XmlAttribute("PortForCollector")]
        public int PortForCollector { get; set; }

        [XmlIgnore]
        public HashFileType Type { get; set; }

        public SavedServerId()
        {
        }

        public SavedServerId(string host, int portForDistributor, int portForCollector)
        {
            Host = host;
            PortForDistributor = portForDistributor;
            PortForCollector = portForCollector;
        }

        public override bool Equals(object obj)
        {
            if (obj is SavedServerId)
            {
                var value = obj as SavedServerId;

                switch (Type)
                {
                    case HashFileType.Distributor:
                        return value.Host == Host && value.PortForDistributor == PortForDistributor;
                    case HashFileType.Collector:
                        return value.Host == Host && value.PortForCollector == PortForCollector;
                    case HashFileType.Controller:
                        return value.Host == Host && value.PortForCollector == PortForCollector &&
                               value.PortForDistributor == PortForDistributor;
                }
                return value.Host == Host && value.PortForDistributor == PortForDistributor;
            }
            var serverId = obj as ServerId;

            switch (Type)
            {
                case HashFileType.Distributor:
                    return serverId.Port == PortForDistributor && serverId.RemoteHost == Host;
                case HashFileType.Collector:
                    return serverId.Port == PortForCollector && serverId.RemoteHost == Host;
            }

            return serverId.Port == PortForDistributor && serverId.RemoteHost == Host;
        }
    }
}
