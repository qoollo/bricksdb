using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Common.NetResults;

namespace Qoollo.Impl.Common.Support
{
    internal static class AggregateResultHelper
    {
        public static RemoteResult AggregateResults(List<RemoteResult> res)
        {
            var l = res.Where(result => result.IsError)
                    .Aggregate("", (current, result) => current + (result.Description + "; "));

            if(string.IsNullOrEmpty(l))
                return new SuccessResult();
            return new FailNetResult(l);
        }
    }
}
