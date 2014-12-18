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
    /// Base class for search
    /// </summary>
    internal abstract class SearchTask:ControlModule
    {
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
            List<FieldDescription> userParametrs, string tableName)
        {
            SearchTasks = new List<SingleServerSearchTask>();
            servers.ForEach(
                x =>
                    SearchTasks.Add(new SingleServerSearchTask(x, script, keyDescription, tableName)
                    {
                        UserParametrs = userParametrs
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
            return SearchTasks.Exists(x => !(x.IsAllDataRead && x.Length==0 || !x.IsServersAvailbale));
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

        public void BackgroundLoadInner(Func<SystemSearchStateInner> getState, IDataLoader loader, Func<OrderSelectTask,List<SingleServerSearchTask>, List<SearchData>> merge)
        {
            try
            {
                bool finish = false;
                while (!_token.IsCancellationRequested && !finish)
                {
                    SearchState = getState();

                    finish = BackgroundLoad(loader, merge);

                    SearchState = getState();

                    if (!_token.IsCancellationRequested && !finish)
                    {
                        _autoResetEvent.Reset();
                        _data.Add(false);
                        _autoResetEvent.WaitOne();
                    }
                    else if(finish)
                    {
                        _data.Add(true);
                    }
                }
                SearchState = getState();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
            }
        }

        protected abstract bool BackgroundLoad(IDataLoader loader, Func<OrderSelectTask, List<SingleServerSearchTask>, List<SearchData>> merge);

        protected override void Dispose(bool isUserCall)
        {
            _token.Cancel();
            _autoResetEvent.Set();
            base.Dispose(isUserCall);
        }
    }
}
