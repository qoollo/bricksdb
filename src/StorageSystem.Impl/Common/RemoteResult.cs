using System.Runtime.Serialization;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.Inner;
using Qoollo.Impl.Common.NetResults.System.Writer;

namespace Qoollo.Impl.Common
{
    [DataContract]
    [KnownType(typeof(SuccessResult))]
    [KnownType(typeof(FailNetResult))]
    [KnownType(typeof(AddDistributorResult))]
    [KnownType(typeof(InitiatorNotAvailableResult))]
    [KnownType(typeof(SystemInfoResult))]
    [KnownType(typeof(InnerServerError))]
    [KnownType(typeof(InnerFailResult))]
    [KnownType(typeof(HashMapResult))]
    [KnownType(typeof(GetRestoreStateResult))]    
    [KnownType(typeof(PackageResult))]    
    public abstract class RemoteResult
    {
        protected RemoteResult(bool result, string description)
        {
            IsError = result;
            Description = description;
        }

        [DataMember]
        public bool IsError { get; private set; }
        [DataMember]
        public string Description { get; private set; }
    }
}
