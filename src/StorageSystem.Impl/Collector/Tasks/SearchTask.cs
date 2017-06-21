using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector.Tasks
{
    /// <summary>
    /// Базовый класс, описывающий поиск
    /// </summary>
    internal abstract class SearchTask : ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public List<SingleServerSearchTask> SearchTasks { get; private set; }
        private ReaderWriterLockSlim _lock;
        public SystemSearchStateInner SearchState
        {
            get
            {
                SystemSearchStateInner ret;
                _lock.EnterReadLock();
                ret = _searchState;
                _lock.ExitReadLock();
                return ret;
            }
            protected set
            {
                _lock.EnterWriteLock();
                _searchState = value;
                _lock.ExitWriteLock();
            }
        }
        private SystemSearchStateInner _searchState;
        private CancellationTokenSource _token;
        private AutoResetEvent _autoResetEvent;
        private BlockingCollection<bool> _data;
        private bool _isFinish;
        private bool _isCanRead;

        protected SearchTask(List<ServerId> servers, FieldDescription keyDescription, string script,
            List<FieldDescription> userParametrs, string tableName, bool isUserScript = false)
        {
            SearchTasks = new List<SingleServerSearchTask>();
            servers.ForEach(
                x =>
                    SearchTasks.Add(new SingleServerSearchTask(x, script, keyDescription, tableName)
                    {
                        UserParametrs = userParametrs,
                        IsUserScript = isUserScript
                    }));

            _lock = new ReaderWriterLockSlim();
            _autoResetEvent = new AutoResetEvent(false);
            _token = new CancellationTokenSource();
            _data = new BlockingCollection<bool>();
            _isFinish = false;
            _isCanRead = true;
        }

        public void ClearServers()
        {
            SearchTasks.RemoveAll(x => !x.IsServersAvailbale);
        }

        #region Can Read

        public bool IsCanRead()
        {
            return _isCanRead;
        }

        public void CalculateCanRead()
        {
            _isCanRead = CalculateCanReadInner();
        }

        public void SetFinish()
        {
            _isCanRead = false;
            _isFinish = true;
        }

        protected bool CalculateCanReadInner()
        {
            return SearchTasks.Exists(x => !(x.IsAllDataRead && x.Length == 0 || !x.IsServersAvailbale));
        }

        #endregion

        public abstract List<SearchData> GetData();

        public List<SearchData> GetDataInner()
        {
            var ret = new List<SearchData>();
            try
            {
                if (!_isFinish)
                {
                    _isFinish = _data.Take(_token.Token);
                    _isCanRead = !_isFinish && IsCanRead();

                    ret = new List<SearchData>(GetData());
                    _autoResetEvent.Set();
                }
            }
            catch (OperationCanceledException)
            {
            }
            return ret;
        }

        public void BackgroundLoadInner(Func<SystemSearchStateInner> getState, IDataLoader loader, Func<OrderSelectTask, List<SingleServerSearchTask>, List<SearchData>> merge)
        {
            try
            {
                bool finish = false;
                while (!_token.IsCancellationRequested && !finish)
                {                    
                    SearchState = getState();

                    finish = BackgroundLoad(loader, merge);
                    _logger.DebugFormat("Load background data. Result = {0}", finish);

                    SearchState = getState();

                    if (!_token.IsCancellationRequested && !finish)
                    {
                        _lock.EnterReadLock();

                        bool action = _isStop;
                        _logger.DebugFormat("Stop state pos 1. Value = {0}", _isStop);

                        _data.Add(false);

                        if (!action)
                            _autoResetEvent.Reset();

                        _lock.ExitReadLock();

                        if (!action)
                            _autoResetEvent.WaitOne();

                        _lock.EnterReadLock();
                        finish = _isStop;
                        _logger.DebugFormat("Stop state pos 2. Value = {0}", _isStop);
                        _lock.ExitReadLock();
                    }
                    else if (finish)
                    {
                        _data.Add(true);
                    }
                }
                SearchState = getState();
            }
            catch (Exception e)
            {
                _logger.Warn(e, "");
            }
            _logger.Info("Finish background merge");
        }

        protected abstract bool BackgroundLoad(IDataLoader loader, Func<OrderSelectTask, List<SingleServerSearchTask>, List<SearchData>> merge);

        private bool _isStop = false;

        protected override void Dispose(bool isUserCall)
        {
            _token.Cancel();

            _lock.EnterWriteLock();
            _isStop = true;
            _lock.ExitWriteLock();

            _autoResetEvent.Set();
            base.Dispose(isUserCall);
        }
    }
}
