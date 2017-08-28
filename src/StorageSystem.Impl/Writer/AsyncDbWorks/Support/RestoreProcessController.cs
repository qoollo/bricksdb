using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{
    internal class RestoreProcessController
    {
        public ServerId RestoreServer
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _restoreServers.FirstOrDefault(x => x.IsCurrentServer);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }
        public List<ServerId> FailedServers
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _restoreServers.Where(x => x.IsFailed).Select(x => (ServerId)x).ToList();
                }
                finally
                {
                    _lock.ExitReadLock();                    
                }                
            }
        }
        public List<RestoreServer> Servers
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return new List<RestoreServer>(_restoreServers);
                }
                finally
                {
                    _lock.ExitReadLock();                    
                }                
            }
        }
        public RestoreState WriterState => _restoreStateHandler.RequiredRestoreState;

        public RestoreProcessController(WriterStateFileLogger saver)
        {
            _saver = saver;
            _restoreServers = new List<RestoreServer>();
            _restoreStateHandler = new RestoreStateHandler(saver);
        }

        private readonly RestoreStateHandler _restoreStateHandler;
        private readonly WriterStateFileLogger _saver;
        private List<RestoreServer> _restoreServers;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void UpdateModel(List<ServerId> servers)
        {
            _lock.EnterWriteLock();

            LocalSendState(RestoreState.FullRestoreNeed);

            _restoreServers.ForEach(x => x.IsFailed = false);
            foreach (var server in servers)
            {
                var s = _restoreServers.FirstOrDefault(x => x.Equals(server));

                if (s != null && !s.IsCurrentServer)
                    _restoreServers.Remove(s);
            }
            
            Save();
            _lock.ExitWriteLock();
        }

        public void SetRestoreDate(RestoreState restoreRunState, RestoreType type, List<RestoreServer> servers)
        {
            SetServers(servers);
            _restoreStateHandler.StartRestore(restoreRunState, type);
            Save();
        }

        #region Servers

        public void SetServers(List<RestoreServer> servers)
        {
            if (_restoreServers.Count > 0)
            {
                foreach (var server in servers)
                {
                    var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
                    if (s != null && server.IsNeedCurrentRestore() && !s.IsServerRestored())
                    {
                        _restoreServers.Remove(s);
                        _restoreServers.Add(server);
                    }

                    if (s == null)
                        _restoreServers.Add(server);
                }

                for (int i = 0; i < _restoreServers.Count;)
                {
                    var s = servers.FirstOrDefault(x => x.Equals(_restoreServers[i]));
                    if (s == null)
                        _restoreServers.RemoveAt(i);
                    else
                        i++;
                }
            }
            else
                _restoreServers = servers;
        }

        public RestoreServer NextRestoreServer()
        {
            _lock.EnterReadLock();
            try
            {
                return _restoreServers.FirstOrDefault(x => x.IsNeedCurrentRestore());
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void ProcessFailedServers()
        {
            var servers = FailedServers;
            _lock.EnterWriteLock();

            foreach (var server in servers)
            {
                var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
                if (s != null)
                    s.AfterFailed();
            }

            Save();
            _lock.ExitWriteLock();            
        }

        public void SetCurrentServer(RestoreServer server)
        {
            _lock.EnterWriteLock();

            server.IsFailed = false;
            server.IsCurrentServer = true;

            Save();
            _lock.ExitWriteLock();
        }

        public void ServerRestored(ServerId server)
        {
            _lock.EnterWriteLock();

            var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
            if (s != null)
                s.IsRestored = true;
            Save();

            _lock.ExitWriteLock();
        }

        public void RemoveCurrentServer()
        {
            _lock.EnterWriteLock();

            var servers = _restoreServers.FirstOrDefault(x => x.IsCurrentServer);
            if (servers != null)
                servers.IsCurrentServer = false;

            Save();
            _lock.ExitWriteLock();
        }

        public void AddServerToFailed(ServerId server)
        {
            _lock.EnterWriteLock();

            var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
            if (s != null)
                s.IsFailed = true;
            Save();

            _lock.ExitWriteLock();
        }

        private bool IsAllServersRestored()
        {
            _lock.EnterReadLock();
            try
            {
                return _restoreServers.All(x => x.IsServerRestored());
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region State

        public void DistributorSendState(RestoreState state)
        {
            LocalSendState(state);
        }

        public bool IsNeedRestore()
        {
            return _restoreStateHandler.IsNeedRestore();
        }

        private void LocalSendState(RestoreState state)
        {
            _restoreStateHandler.TryUpdateState(state);
        }

        public void FinishRestore()
        {
            if (!IsAllServersRestored())
                return;

            _lock.EnterWriteLock();
            try
            {
                _restoreStateHandler.CompleteRestore();

                _restoreServers.Clear();
                Save();
                _saver?.RemoveFile();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        private void Save()
        {
            _saver?.Save();
        }

        public WriterStateDataContainer GetState()
        {
            return new WriterStateDataContainer(WriterState, Servers);
        }
    }
}
