using System;
using System.Xml.Serialization;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.HashFile
{
    [Serializable]
    public class HashMapRecord
    {
        [XmlAttribute("Begin")]
        public string Begin { get;  set; }
        [XmlAttribute("End")]
        public string End { get;  set; }        
        
        public SavedServerId Save { get; set; }
        [XmlIgnore]
        public ControllerDescription ServerId { get; private set; }

        public HashMapRecord()
        {
        }

        public HashMapRecord(string begin, string end)
        {
            if (begin.Length > 24) begin = begin.Remove(0, begin.Length - 24);
            if (end.Length > 24) end = end.Remove(0, end.Length - 24);
            Begin = begin;
            End = end;
            Save = new SavedServerId("", 1, 1);
        }

        public bool IsMine(string hash)
        {
            return HashComparer.Compare(Begin, hash) <= 0 && HashComparer.Compare(hash, End) <= 0;
        }

        public void Prepare(HashFileType type)
        {
            Save.Type = type;
            ServerId = type == HashFileType.Collector
                ? new ControllerDescription(Save.Host, Save.PortForCollector)
                : new ControllerDescription(Save.Host, Save.PortForDistributor);
        }

        public void SetServer(ControllerDescription server)
        {
            ServerId = server;
        }

        public override int GetHashCode()
        {
            return Begin.GetHashCode() ^ End.GetHashCode();
        }

        public HashMapRecord Clone()
        {
            return new HashMapRecord(Begin, End) {Save = Save};
        }
    }
}
