using System;
using System.ServiceModel;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.NetInterfaces.Writer
{
    [ServiceContract]
    internal interface ISearchNet
    {
        [OperationContract]
        Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description);
    }
}
