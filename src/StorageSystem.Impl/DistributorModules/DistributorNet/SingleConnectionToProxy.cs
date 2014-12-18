using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class SingleConnectionToProxy : SingleConnection<ICommonCommunicationNet>, ICommonCommunicationNet, ISingleConnection
    {
        public SingleConnectionToProxy(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonCommunicationNet>(
               api => api.SendSync(command),
               e => new ServerNotAvailable(Server),
               NetLogHelper.GetLog(command));
        }        

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonCommunicationNet>(
               api => api.Ping(),
               e => new ServerNotAvailable(Server),
               "");
        }

        public RemoteResult SendASyncWithResult(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonCommunicationNet>(
                api =>
                {
                    api.SendASync(command);
                    return new SuccessResult();
                },
               e => new ServerNotAvailable(Server),
               NetLogHelper.GetLog(command));
        }

        #region Not Implemented

        public void SendASync(NetCommand command)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
