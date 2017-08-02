﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Ninject;
using Qoollo.Impl.Common.Exceptions;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.HashModule;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer
{
    internal class WriterModel : ControlModule, IWriterModel
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private HashMap _map;
        private List<HashMapRecord> _localMap;
        private readonly ServerId _local;
        private int _countReplics;

        public List<HashMapRecord> LocalMap
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _localMap;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public ServerId Local => _local;

        public List<ServerId> Servers
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _map.Servers.Select(x => new ServerId(x)).ToList();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public List<ServerId> OtherServers
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _map.Servers
                        .Select(x => new ServerId(x))
                        .Where(x => !x.Equals(_local))
                        .ToList();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public WriterModel(StandardKernel kernel, ServerId local) :base(kernel)
        {
            Contract.Requires(local != null);

            _local = local;
        }

        public override void Start()
        {
            _countReplics = Kernel.Get<ICommonConfiguration>().CountReplics;

            _map = new HashMap(Kernel, HashFileType.Writer);

            _map.Start();
            _map.CreateMap();

            _localMap = _map.GetHashMap(_local);
            if (_localMap.Count == 0)
                throw new Exception("There is no server in hash file with our address");
        }

        public bool IsMine(string hash)
        {
            _lock.EnterReadLock();
            try
            {
                return _localMap.Exists(x => x.IsMine(hash));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void UpdateModel()
        {
            _lock.EnterWriteLock();
            try
            {
                _map.UpdateFileModel();
                _localMap = _map.GetHashMap(_local);
                if (_localMap.Count == 0)
                    throw new InitializationException("There is no server in hash file with our address");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public string UpdateHashViaNet(List<HashMapRecord> map)
        {
            var currentMap = _map.Map;
            bool equal = true;
            currentMap.ForEach(x =>
            {
                if (!map.Contains(x))
                    equal = false;
            });

            map.ForEach(x =>
            {
                if (!currentMap.Contains(x))
                    equal = false;
            });

            if (equal)
                return string.Empty;


            _lock.EnterWriteLock();
            
            try
            {
                HashFileUpdater.UpdateFile(_map.Filename);

                _map.CreateNewMapWithFile(map);

                UpdateModel();
            }
            catch (InitializationException e)
            {
                return e.Message;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return string.Empty;
        }

        public List<HashMapRecord> GetHashMap(ServerId server)
        {
            _lock.EnterReadLock();
            try
            {
                var writerServer = _map.Servers.FirstOrDefault(s => s.Equals(server));
                if (writerServer == null)
                {
                    if(_logger.IsWarnEnabled)
                        _logger.Warn($"Cant find server in model: {server}");
                    return null;
                }
                return _map.GetHashMap(server);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<ServerId> GetDestination(string hash)
        {
            _lock.EnterReadLock();
            try
            {
                return HashLogic.GetDestination(_countReplics, hash, _map.AvailableMap);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
                _map.Dispose();
        }
    }
}
