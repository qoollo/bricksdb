using System.Runtime.Serialization;
using Qoollo.Impl.Common.NetResults.System.Collector;
using Qoollo.Impl.Common.NetResults.System.DbController;
using Qoollo.Impl.Common.NetResults.System.Distributor;

namespace Qoollo.Impl.Common
{
    [DataContract]
    [KnownType(typeof(AddDistributorFromDistributorCommand))]
    [KnownType(typeof(TakeInfoCommand))]
    [KnownType(typeof(RestoreCommand))]
    [KnownType(typeof(RestoreCommandWithData))]
    [KnownType(typeof(RestoreInProcessCommand))]
    [KnownType(typeof(RestoreCompleteCommand))]
    [KnownType(typeof(OperationCompleteCommand))]
    [KnownType(typeof(ReadOperationCompleteCommand))]
    [KnownType(typeof(IsRestoredCommand))]
    [KnownType(typeof(GetHashMapCommand))]    
    internal class NetCommand
    {
    }
}
