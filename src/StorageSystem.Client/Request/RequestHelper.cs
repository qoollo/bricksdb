using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Client.Request
{
    internal static class RequestHelper
    {
        public static RequestState GetStatefromRemoteResult(RemoteResult result)
        {
            if(result.IsError)
                return RequestState.Error;
            
            return RequestState.Complete;
        }

        public static bool IsErrorString(string s)
        {
            if (s == Errors.RestoreAlreadyStarted)
                return true;

            if (s == Errors.RestoreFailConnectToDistributor)
                return true;

            if (s == Errors.FailRead)
                return true;

            if (s == Errors.RestoreStartedWithoutErrors)
                return false;

            if (s.Contains(Errors.ServerIsNotAvailable.Split(' ')[0]) &&
                s.Contains(Errors.ServerIsNotAvailable.Split(' ')[1]))
                return true;

            return false;
        }
    }
}
