using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Pools.BalancedPool;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Turbo;

namespace Qoollo.Impl.Modules.Net.ConnectionBehavior
{
    internal class WpfConnection<TConnection> : ConnectionBehaviorBase<TConnection>
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();
        private readonly StableElementsDynamicConnectionPool<TConnection> _bPool;

        public WpfConnection(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server)
        {
            _bPool =
                new StableElementsDynamicConnectionPool<TConnection>(
                    factory: NetConnector.Connect<TConnection>(server, configuration.ServiceName, timeoutConfiguration),
                    maxAsyncQueryCount: 1,
                    maxElementCount: configuration.MaxElementCount,
                    trimPeriod: configuration.TrimPeriod, 
                    name: "Connection pool to " + server,
                    minCount: configuration.MaxElementCount);
        }

        public override bool Connect()
        {
            var ret = false;
            try
            {
                using (var val = _bPool.Rent(10000))
                {
                    if (val.IsValid)
                        ret = val.Element.State == StableConnectionState.Opened;
                }
            }
            catch (Exception e)
            {
                if (_logger.IsWarnEnabled)
                    _logger.WarnFormat(e, $"Failed open connection to {Server}");
            }

            return ret;
        }

        public override TResult SendFunc<TResult, TApi>(Func<TApi, TResult> func, Func<Exception, TResult> errorRet, string errorLogFromData)
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
                                throw new CommunicationException(
                                    "Connection can't be used for communications. Target: " +
                                    Server);

                            res = func(request.API as TApi);
                        }
                    }
                    catch (EndpointNotFoundException e)
                    {
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                    catch (CommunicationException e)
                    {
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                    catch (CantRetrieveElementException e)
                    {
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                    catch (TimeoutException e)
                    {
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        res = errorRet(e);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                res = errorRet(e);
            }


            return res;
        }

        public override Task<TResult> SendAsyncFunc<TResult, TApi>(Func<TApi, Task<TResult>> func, Func<Exception, TResult> errorRet, string errorLogFromData)
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

                        var request = default(ConcurrentRequestTracker<TConnection>);
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
                                        _logger.ErrorFormat(originalTask.Exception.GetBaseException(),
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
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                    catch (CommunicationException e)
                    {
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                    catch (CantRetrieveElementException e)
                    {
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                    catch (TimeoutException e)
                    {
                        _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                        finalTask.SetResult(errorRet(e));
                    }
                }
            }
            catch (Exception e)
            {
                _logger.ErrorFormat(e, "Server = {0}, message = {1}", Server, errorLogFromData);
                finalTask.SetResult(errorRet(e));
            }

            return finalTask.Task;
        }

        protected override void Dispose(bool isUserCall)
        {            
            _bPool.Dispose();
        }
    }
}