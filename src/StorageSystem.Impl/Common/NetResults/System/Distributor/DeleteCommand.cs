using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class DeleteCommand:NetCommand
    {
        [DataMember]
        public string Command { get; set; }

        public DeleteCommand(string command)
        {
            Contract.Requires(!string.IsNullOrEmpty(command));
            Command = command;
        }
    }
}
