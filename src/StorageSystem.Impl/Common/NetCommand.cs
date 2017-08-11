using System.Runtime.Serialization;
using Qoollo.Impl.Common.NetResults.System.Collector;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;

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
    [KnownType(typeof(GetHashMapCommand))]
    [KnownType(typeof(SetRestoreStateCommand))]    
    [KnownType(typeof(HashFileUpdateCommand))]
    [KnownType(typeof(RestoreFromDistributorCommand))]
    [KnownType(typeof(DeleteCommand))]    
    internal class NetCommand
    {
    }
}
