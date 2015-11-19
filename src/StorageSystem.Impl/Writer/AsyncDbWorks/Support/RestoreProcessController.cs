using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{
    internal class RestoreProcessController
    {
        public ServerId RestoreServer
        {
            get
            {
                _reader.EnterReadLock();
                try
                {
                    return _restoreServers.FirstOrDefault(x => x.IsCurrentServer);
                }
                finally
                {
                    _reader.ExitReadLock();
                }
            }
        }

        public List<ServerId> FailedServers
        {
            get
            {
                _reader.EnterReadLock();
                try
                {
                    return _restoreServers.Where(x => x.IsFailed).Select(x => (ServerId)x).ToList();
                }
                finally
                {
                    _reader.ExitReadLock();                    
                }                
            }
        }

        public List<RestoreServer> Servers
        {
            get
            {
                _reader.EnterReadLock();
                try
                {
                    return new List<RestoreServer>(_restoreServers);
                }
                finally
                {
                    _reader.ExitReadLock();                    
                }                
            }
        }

        public RestoreProcessController(RestoreStateFileLogger saver)
        {
            _saver = saver;
        }        

        private readonly RestoreStateFileLogger _saver;
        private List<RestoreServer> _restoreServers;
        private readonly ReaderWriterLockSlim _reader = new ReaderWriterLockSlim();

        public void SetServers(List<ServerId> servers)
        {
            _restoreServers = servers.Select(x => new RestoreServer(x)).ToList();
        }

        public void SetServers(List<RestoreServer> servers)
        {
            _restoreServers = servers;
        }

        public void UpdateModel(List<ServerId> servers)
        {
            _reader.EnterWriteLock();

            _restoreServers.ForEach(x => x.IsFailed = false);

            foreach (var server in servers)
            {
                var s = _restoreServers.FirstOrDefault(x => x.Equals(server));

                if (s != null && !s.IsCurrentServer)
                    _restoreServers.Remove(s);
            }

            Save();
            _reader.ExitWriteLock();
        }

        public RestoreServer NextRestoreServer()
        {
            _reader.EnterReadLock();
            try
            {
                return _restoreServers.FirstOrDefault(x => x.IsNeedCurrentRestore());
            }
            finally
            {
                _reader.ExitReadLock();
            }
        }

        public void ProcessFailedServers()
        {
            _reader.EnterWriteLock();

            foreach (var server in FailedServers)
            {
                var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
                if (s != null)
                    s.AfterFailed();
            }

            _reader.ExitWriteLock();
            Save();
        }

        public void ChangeCurrentServer()
        {
            _reader.EnterWriteLock();

            var servers = _restoreServers.FirstOrDefault(x => x.IsCurrentServer);
            if (servers != null)
                servers.IsCurrentServer = false;

            _reader.ExitWriteLock();
        }

        public void AddServerToFailed(ServerId server)
        {
            _reader.EnterWriteLock();

            var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
            if (s != null)
                s.IsFailed = true;

            _reader.ExitWriteLock();
        }

        public void Save()
        {
            if (_saver != null)
                _saver.Save();
        }
    }
}
