using System;
using System.Diagnostics.Contracts;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Impl.Modules.Pools.BalancedPool
{
    internal struct ConcurrentRequestTracker<TApi> : IDisposable
    {
        private readonly ConcurrentRequestTracker _inner;

        public ConcurrentRequestTracker(ConcurrentRequestTracker innerTracker)
        {
            Contract.Requires(innerTracker != null);

            _inner = innerTracker;
        }

        public TApi API { get { return (TApi)_inner.Channel; } }
        public bool CanBeUsedForCommunication { get { return _inner.CanBeUsedForCommunication; } }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }

    internal class StableConcurrentConnection<TApi> : IDisposable
    {
        private const int InitialOpenConnectionPause = 1000;
        private const int MaxOpenConnectionPause = 180000;

        private readonly ChannelFactory<TApi> _factory;
        private readonly string _targetName;
        private readonly int _maxAsyncQueryCount;
        private readonly ManualResetEventSlim _openWaiter;
        private ConcurrentConnection _channel;
        private volatile bool _isDisposed;
        private int _openConnectionPauseMs = InitialOpenConnectionPause;


        public StableConcurrentConnection(ChannelFactory<TApi> factory, int maxAsyncQueryCount, bool syncFirstOpen = false)
        {
            Contract.Requires(factory != null);

            _factory = factory;
            _maxAsyncQueryCount = maxAsyncQueryCount;
            _targetName = factory.Endpoint.Address.ToString();
            _openWaiter = new ManualResetEventSlim();
            _isDisposed = false;

            ReinitChannel(syncFirstOpen);
        }

        public StableConnectionState State { get { return _channel.State; } }
        public bool CanBeUsedForCommunication { get { return _channel.CanBeUsedForCommunication; } }
        public int ConcurrentRequestCount { get { return _channel.ConcurrentRequestCount; } }

        protected virtual void OnConnectionRecreated()
        {
        }
        protected virtual void OnConnectionOpened()
        {
        }
        protected virtual void OnConnectionClosed()
        {
        }

        private void ReinitChannel(bool isSync = false)
        {
            var oldChannel = _channel;
            if (oldChannel != null)
            {
                oldChannel.Closed -= ChannelClosedHandler;
                oldChannel.Opened -= ChannelOpenedHandler;
            }


            var newChannel = new ConcurrentConnection((IClientChannel)_factory.CreateChannel(), _maxAsyncQueryCount);

            if (Interlocked.CompareExchange(ref _channel, newChannel, oldChannel) != oldChannel || _isDisposed)
            {
                newChannel.Dispose();
            }
            else
            {
                _openWaiter.Reset();
                OnConnectionRecreated();
                newChannel.Closed += ChannelClosedHandler;
                newChannel.Opened += ChannelOpenedHandler;
                if (isSync)
                    newChannel.Open();
                else
                    newChannel.OpenAsync();
            }

            if (oldChannel != null)
                oldChannel.Dispose();
        }

        private void ChannelClosedHandler(object sender, EventArgs e)
        {
            OnConnectionClosed();

            if (!_isDisposed)
            {
                _openWaiter.Reset();

                Logger.Logger.Instance.Warn("Connection was closed, due to some error. Target: " + _targetName);

                _openConnectionPauseMs = 2 * _openConnectionPauseMs;
                if (_openConnectionPauseMs > MaxOpenConnectionPause)
                    _openConnectionPauseMs = MaxOpenConnectionPause;

                Task.Delay(_openConnectionPauseMs).ContinueWith((prev) =>
                {
                    ReinitChannel();
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }
        private void ChannelOpenedHandler(object sender, EventArgs e)
        {
            _openConnectionPauseMs = InitialOpenConnectionPause;
            OnConnectionOpened();
            _openWaiter.Set();
        }


        public ConcurrentRequestTracker<TApi> RunRequest(int timeout)
        {
            return new ConcurrentRequestTracker<TApi>(_channel.RunRequest(timeout));
        }



        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                var oldChannel = _channel;
                if (oldChannel != null)
                {
                    oldChannel.Closed -= ChannelClosedHandler;
                    oldChannel.Opened -= ChannelOpenedHandler;
                    oldChannel.Dispose();
                }
                _openWaiter.Dispose();
            }
        }
    }
}
