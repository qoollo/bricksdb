using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{
    [DataContract]
    internal class PackageResult : SuccessResult
    {
        [DataMember]
        public bool[] Result { get; set; }

        public PackageResult(bool[] result)
        {
            Result = result;
        }
    }
}
