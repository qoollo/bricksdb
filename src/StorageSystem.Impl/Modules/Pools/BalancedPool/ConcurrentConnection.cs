using System;
using System.Diagnostics.Contracts;
using System.ServiceModel;
using System.Threading;

namespace Qoollo.Impl.Modules.Pools.BalancedPool
{
    /// <summary>
    /// Channel state
    /// </summary>
    internal enum StableConnectionState
    {
        Created,
        Opening,
        Opened,
        Closed,
        Invalid
    }

    internal class ConcurrentRequestTracker : IDisposable
    {
        private static readonly ConcurrentRequestTracker _empty = new ConcurrentRequestTracker();
        public static ConcurrentRequestTracker Empty { get { return _empty; } }

        // ==========

        private ConcurrentConnection _connectionSource;

        protected ConcurrentRequestTracker()
        {
            _connectionSource = null;
        }

        public ConcurrentRequestTracker(ConcurrentConnection conSrc)
        {
            Contract.Requires(conSrc != null);

            _connectionSource = conSrc;
        }

        public IClientChannel Channel { get { return _connectionSource.Channel; } }
        public StableConnectionState State
        {
            get
            {
                var tmp = _connectionSource;
                if (tmp != null)
                    return tmp.State;
                return StableConnectionState.Invalid;
            }
        }
        public bool CanBeUsedForCommunication { get { return State == StableConnectionState.Opened; } }


        private void Dispose(bool isUserCall)
        {
            var tmp = Interlocked.Exchange(ref _connectionSource, null);
            if (tmp != null)
                tmp.FinishRequest();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConcurrentRequestTracker()
        {
            Dispose(false);
        }
    }


    internal class ConcurrentConnection: IDisposable
    {
        private readonly IClientChannel _channel;
        private readonly string _targetName;

        private readonly int _maxAsyncQueryCount;
        private readonly SemaphoreSlim _concurrencyTrackSemaphore;
        private int _concurrenyRequestCount;

        private int _state;
        private volatile bool _isDisposeRequested;
        private volatile bool _isDisposed;

        private readonly TimeSpan _operationTimeout;

        public ConcurrentConnection(IClientChannel channel, int maxAsyncQueryCount)
        {
            Contract.Requires(channel != null);

            _channel = channel;
            _channel.Faulted += ChannelFaultedHandler;
            _channel.Closed += ChannelClosedExternallyHandler;
            _targetName = _channel.RemoteAddress.ToString();
            _operationTimeout = _channel.OperationTimeout;

            _concurrenyRequestCount = 0;
            _maxAsyncQueryCount = 0;
            if (maxAsyncQueryCount > 0)
            {
                _maxAsyncQueryCount = maxAsyncQueryCount;
                _concurrencyTrackSemaphore = new SemaphoreSlim(_maxAsyncQueryCount);
            }

            _state = (int)StableConnectionState.Created;
            _isDisposed = false;
            _isDisposeRequested = false;
        }


        public IClientChannel Channel { get { return _channel; } }
        public string TargetName { get { return _targetName; } }
        public StableConnectionState State { get { return (StableConnectionState)_state; } }
        public bool CanBeUsedForCommunication { get { return State == StableConnectionState.Opened; } }

        public int ConcurrentRequestCount { get { return _concurrenyRequestCount; } }

        public event EventHandler Closed;
        public event EventHandler Opened;



        private bool IsValidStateTransition(StableConnectionState oldState, StableConnectionState newState)
        {
            switch (oldState)
            {
                case StableConnectionState.Created:
                    return newState == StableConnectionState.Opening || newState == StableConnectionState.Closed;
                case StableConnectionState.Opening:
                    return newState == StableConnectionState.Opened || newState == StableConnectionState.Closed;
                case StableConnectionState.Opened:
                    return newState == StableConnectionState.Closed;
                case StableConnectionState.Closed:
                    return newState == StableConnectionState.Invalid;
                case StableConnectionState.Invalid:
                    return false;
                default:
                    throw new InvalidProgramException("Unknown StableConnectionState value: " + oldState.ToString());
            }
        }

        private StableConnectionState ChangeStateSafe(StableConnectionState newState)
        {
            var curState = _state;

            if (!IsValidStateTransition((StableConnectionState)curState, newState))
                return (StableConnectionState)curState;

            if (Interlocked.CompareExchange(ref _state, (int)newState, curState) == curState)
                return (StableConnectionState)curState;

            SpinWait sw = new SpinWait();
            while (Interlocked.CompareExchange(ref _state, (int)newState, curState) != curState)
            {
                sw.SpinOnce();
                curState = _state;
                if (!IsValidStateTransition((StableConnectionState)curState, newState))
                    return (StableConnectionState)curState;
            }

            return (StableConnectionState)curState;
        }


        private void OnClosed()
        {
            if (ChangeStateSafe(StableConnectionState.Closed) == StableConnectionState.Closed)
                return;

            var tmp = Closed;
            if (tmp != null)
                tmp(this, EventArgs.Empty);

            TryFreeAllResources();
        }

        private void OnOpened()
        {
            if (ChangeStateSafe(StableConnectionState.Opened) != StableConnectionState.Opening)
                return;

            var tmp = Opened;
            if (tmp != null)
                tmp(this, EventArgs.Empty);
        }


        private void ChannelFaultedHandler(object sender, EventArgs e)
        {
            OnClosed();
        }
        private void ChannelClosedExternallyHandler(object sender, EventArgs e)
        {
            OnClosed();
        }


        public bool Open(int timeout)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (ChangeStateSafe(StableConnectionState.Opening) != StableConnectionState.Created)
                throw new InvalidOperationException("Can't open channel which is in state: " + _state.ToString());

            try
            {
                if (timeout < 0)
                    _channel.Open();
                else
                    _channel.Open(TimeSpan.FromMilliseconds(timeout));

                if (_channel.State == CommunicationState.Opened)
                    OnOpened();
                else
                    Close();
            }
            catch (CommunicationException cex)
            {
                Logger.Logger.Instance.Debug(cex, "Error during opening the connection to " + TargetName);
                OnClosed();
            }
            catch (TimeoutException tex)
            {
                Logger.Logger.Instance.Debug(tex, "Error during opening the connection to " + TargetName);
                OnClosed();
            }

            return State == StableConnectionState.Opened;
        }
        public bool Open()
        {
            return this.Open(-1);
        }


        public void OpenAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (ChangeStateSafe(StableConnectionState.Opening) != StableConnectionState.Created)
                throw new InvalidOperationException("Can't open channel, that is in state: " + _state.ToString());

            try
            {
                _channel.BeginOpen(new AsyncCallback(OpenedAsyncHandler), null);
            }
            catch (CommunicationException cex)
            {
                Logger.Logger.Instance.Debug(cex, "Error during opening the connection to " + TargetName);
                OnClosed();
            }
            catch (TimeoutException tex)
            {
                Logger.Logger.Instance.Debug(tex, "Error during opening the connection to " + TargetName);
                OnClosed();
            }
        }

        private void OpenedAsyncHandler(IAsyncResult res)
        {
            try
            {
                _channel.EndOpen(res);
                if (_channel.State == CommunicationState.Opened)
                    OnOpened();
                else
                    Close();
            }
            catch (CommunicationException cex)
            {
                Logger.Logger.Instance.Debug(cex, "Error during opening the connection to " + TargetName);
                OnClosed();
            }
            catch (TimeoutException tex)
            {
                Logger.Logger.Instance.Debug(tex, "Error during opening the connection to " + TargetName);
                OnClosed();
            }
        }


        public ConcurrentRequestTracker RunRequest(int timeout)
        {
            if (_isDisposed)
                return ConcurrentRequestTracker.Empty;

            if (_concurrencyTrackSemaphore != null)
            {
                if (!_concurrencyTrackSemaphore.Wait(timeout))
                {
                    Logger.Logger.Instance.Debug("RunRequest failed due to timeout in waiting for concurrent resource. Current Timeout: " + timeout.ToString() + " ms, Target: " + _targetName);
                    throw new TimeoutException("RunRequest failed due to timeout in waiting for concurrent resource. Current Timeout: " + timeout.ToString() + " ms, Target: " + _targetName);
                }
            }

            Interlocked.Increment(ref _concurrenyRequestCount);
            return new ConcurrentRequestTracker(this);
        }


        public void FinishRequest()
        {
            if (_isDisposed)
                return;

            if (_concurrencyTrackSemaphore != null)
                _concurrencyTrackSemaphore.Release();

            Interlocked.Decrement(ref _concurrenyRequestCount);

            TryFreeAllResources();
        }



        private void Close()
        {
            try
            {
                _channel.Faulted -= ChannelFaultedHandler;
                _channel.Closed -= ChannelClosedExternallyHandler;

                if (_channel.State == CommunicationState.Opened)
                    _channel.Close();
                else
                    _channel.Abort();
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Debug(ex, "WCF channel close exception.");
                _channel.Abort();
            }
            finally
            {
                OnClosed();
            }
        }
        private void CloseFromFinalizer()
        {
            try
            {
                _channel.Faulted -= ChannelFaultedHandler;
                _channel.Closed -= ChannelClosedExternallyHandler;

                if (_channel != null)
                    _channel.Abort();
            }
            catch
            {
            }
            finally
            {
                ChangeStateSafe(StableConnectionState.Closed);
            }
        }



        private void FreeAllResources()
        {
            if (!_isDisposed)
            {
                _isDisposeRequested = true;
                _isDisposed = true;

                var prevState = ChangeStateSafe(StableConnectionState.Invalid);
                Contract.Assume(prevState == StableConnectionState.Closed);

                if (_concurrencyTrackSemaphore != null)
                    _concurrencyTrackSemaphore.Dispose();

                try
                {
                    if (_channel.State == CommunicationState.Faulted)
                        _channel.Abort();
                    _channel.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Logger.Instance.Debug(ex, "WCF channel close exception.");
                }
            }
        }
        private void TryFreeAllResources()
        {
            if (_isDisposeRequested && !_isDisposed && State == StableConnectionState.Closed &&
                (_concurrencyTrackSemaphore == null || _concurrencyTrackSemaphore.CurrentCount == _maxAsyncQueryCount))
            {
                FreeAllResources();
            }
        }


        protected virtual void Dispose(bool isUserCall)
        {
            //Contract.Assume(isUserCall == true);

            _isDisposeRequested = true;

            if (isUserCall)
            {
                if (!_isDisposed)
                    Close();
            }
            else
            {
                if (!_isDisposed)
                {
                    CloseFromFinalizer();
                    FreeAllResources();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConcurrentConnection()
        {
            Dispose(false);
        }
    }
}
