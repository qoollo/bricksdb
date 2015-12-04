using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Pools.BalancedPool;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Turbo;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class SingleConnection<T> : ControlModule
    {
        private readonly StableElementsDynamicConnectionPool<T> _bPool;

        public ServerId Server { get; private set; }

        protected SingleConnection(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration)
        {
            Server = server;
            _bPool =
                new StableElementsDynamicConnectionPool<T>(
                    NetConnector.Connect<T>(Server, configuration.ServiceName, timeoutConfiguration),
                    1, configuration.MaxElementCount, "Connection pool to " + Server);
        }

        public bool Connect()
        {
            bool ret = false;
            try
            {
                using (var val = _bPool.Rent(10000))
                {
                    if (val.IsValid)
                        ret = val.Element.State == StableConnectionState.Opened;
                }
            }
            catch (Exception)
            {

            }


            return ret;
        }

        protected TResult SendFunc<TResult, TApi>(Func<TApi, TResult> func, Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class
        {
            TResult res;
            try
            {
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
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                    catch (CommunicationException e)
                    {
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                    catch (CantRetrieveElementException e)
                    {
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                    catch (TimeoutException e)
                    {
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                res = errorRet(e);
            }


            return res;
        }

        protected Task<TResult> SendAsyncFunc<TResult, TApi>(Func<TApi, Task<TResult>> func,
            Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class
        {
            var finalTask = new TaskCompletionSource<TResult>();
            try
            {
                using (var elem = _bPool.Rent())
                {
                    try
                    {
                        if (!elem.Element.CanBeUsedForCommunication)
                            throw new CommunicationException("Connection can't be used for communications. Target: " +
                                                             Server);

                        var request = default(ConcurrentRequestTracker<T>);
                        bool needFreeRequest = true;

                        try
                        {
                            request = elem.Element.RunRequest(_bPool.DeadlockTimeout);

                            if (!request.CanBeUsedForCommunication)
                                throw new CommunicationException(
                                    "Connection can't be used for communications. Target: " + Server);

                            var originalTask = func(request.API as TApi);
                            elem.Dispose();

                            originalTask.ContinueWith(tsk =>
                            {
                                request.Dispose();
                            }, TaskContinuationOptions.ExecuteSynchronously)
                                .ContinueWith(tsk =>
                                {
                                    if (originalTask.IsCanceled)
                                        finalTask.SetCanceled();
                                    else if (originalTask.IsFaulted)
                                    {
                                        Logger.Logger.Instance.ErrorFormat(originalTask.Exception.GetBaseException(),
                                            "Server = {0}, message = {1}", Server, errorLogFromData);
                                        finalTask.SetResult(errorRet(originalTask.Exception.GetBaseException()));
                                    }
                                    else
                                        finalTask.SetResult(originalTask.Result);
                                });
                            needFreeRequest = false;
                        }
                        finally
                        {
                            if (needFreeRequest)
                                request.Dispose();
                        }

                    }
                    catch (EndpointNotFoundException e)
                    {
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                    catch (CommunicationException e)
                    {
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                    catch (CantRetrieveElementException e)
                    {
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                    catch (TimeoutException e)
                    {
                        Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                finalTask.SetResult(errorRet(e));
            }

            return finalTask.Task;
        }


        protected override void Dispose(bool isUserCall)
        {
            Logger.Logger.Instance.DebugFormat("Dispose for {0}", Server);
            _bPool.Dispose();
            
            base.Dispose(isUserCall);
        }
    }
}
