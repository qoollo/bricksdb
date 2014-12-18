using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.NetResults.Event
{
    [DataContract]
    internal class HashMapResult:SuccessResult
    {
        [DataMember]
        public List<Tuple<ServerId, string, string>> Servers { get; private set; }

        public HashMapResult(List<HashMapRecord> hash)
        {
            Servers =
                new List<Tuple<ServerId, string, string>>(
                    hash.Select(x => new Tuple<ServerId, string, string>(new ServerId(x.ServerId), x.Begin, x.End)));
        }
    }
}
