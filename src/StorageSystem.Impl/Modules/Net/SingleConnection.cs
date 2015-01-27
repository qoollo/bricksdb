using System;
using System.ServiceModel;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Pools.BalancedPool;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Turbo;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class SingleConnection<T>:ControlModule
    {
        private StableElementsDynamicConnectionPool<T> _bPool;
        public ServerId Server { get; private set; }

        protected SingleConnection(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration)
        {
            Server = server;
            _bPool =
                new StableElementsDynamicConnectionPool<T>(
                    NetConnector.Connect<T>(Server, configuration.ServiceName, timeoutConfiguration),
                    -1, configuration.MaxElementCount);
        }

        public bool Connect()
        {
            bool ret = false;
            using (var val = _bPool.Rent(10000))
            {
                if (val.IsValid)
                    ret = val.Element.State == StableConnectionState.Opened;
            }

            return ret;
        }

        protected TResult SendFunc<TResult, TApi>(Func<TApi, TResult> func, Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class
        {
            TResult res;
            using (var elem = _bPool.Rent())
            {
                try
                {
                    if (!elem.Element.CanBeUsedForCommunication)
                        throw new CommunicationException("Connection can't be used for communications. Target: " +
                                                         Server);

                    using (var request = elem.Element.RunRequest(_bPool.DeadlockTimeout))
                    {
                        if (!request.CanBeUsedForCommunication)
                            throw new CommunicationException("Connection can't be used for communications. Target: " +
                                                             Server);

                        res = func(request.API as TApi);
                    }
                }
                catch (EndpointNotFoundException e)
                {
                    Logger.Logger.Instance.ErrorFormat(e, "message = {0}", errorLogFromData);
                    res = errorRet(e);
                }
                catch (CommunicationException e)
                {
                    Logger.Logger.Instance.ErrorFormat(e, "message = {0}", errorLogFromData);
                    res = errorRet(e);
                }
                catch (CantRetrieveElementException e)
                {
                    Logger.Logger.Instance.ErrorFormat(e, "message = {0}", errorLogFromData);
                    res = errorRet(e);
                }
                catch (TimeoutException e)
                {
                    Logger.Logger.Instance.ErrorFormat(e, "message = {0}", errorLogFromData);
                    res = errorRet(e);
                }
            }
            return res;
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _bPool.Dispose();
            }
            base.Dispose(isUserCall);
        }        
    }
}
