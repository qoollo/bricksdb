using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.HashModule;

namespace Qoollo.Impl.DistributorModules.Model
{
    internal class WriterSystemModel:ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public List<WriterDescription> Servers { get { return new List<WriterDescription>(_servers); } } 

        public WriterSystemModel(StandardKernel kernel, int countReplics)
            :base(kernel)
        {
            _countReplics = countReplics;

            _lock = new ReaderWriterLockSlim();
            _servers = new List<WriterDescription>();
            _map = new HashMap(kernel, HashFileType.Distributor);
        }

        private List<WriterDescription> _servers;
        private readonly int _countReplics;
        private readonly ReaderWriterLockSlim _lock;
        private readonly HashMap _map;

        public void ServerNotAvailable(ServerId serverId)
        {
            _lock.EnterWriteLock();

            var server = _servers.FirstOrDefault(x => x.Equals(serverId));

            if (server == null)
            {
                _logger.ErrorFormat(
                    "Server {0} is missing in model of this file, but command received that it is unavailable", serverId);
            }
            else
            {
                server.NotAvailable();
            }
            _map.CreateAvailableMap();
            _lock.ExitWriteLock();
        }

        public void ServerAvailable(ServerId serverId)
        {
            _lock.EnterWriteLock();
            
            var server = _servers.FirstOrDefault(x => x.Equals(serverId));

            if (server == null)
            {
                _logger.ErrorFormat(
                    "Server {0} is missing in model of this file, but command received that it is unavailable", serverId);
            }
            else
            {
                server.Available();
            }
            _map.CreateAvailableMap();
            _lock.ExitWriteLock();
        }

        public override void Start()
        {
            _map.Start();
            _map.CreateMap();
            _servers = _map.Servers;
        }

        public void UpdateFromFile()
        {
            _map.UpdateFileModel();
            _servers = _map.Servers;
        }

        public void UpdateModel()
        {
            _servers.ForEach(x=>x.UpdateModel());
        }

        public void UpdateHashViaNet(List<HashMapRecord> map)
        {
            var currentMap = GetAllServersForCollector();
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
                return;

            HashFileUpdater.UpdateFile(_map.Filename);
            _map.CreateNewMapWithFile(map);
            _map.CreateAvailableMap();
        }

        public List<WriterDescription> GetDestination(InnerData ev)
        {
            _lock.EnterReadLock();
            try
            {
                return HashLogic.GetDestination(_countReplics, ev.Transaction.DataHash,
                    _map.AvailableMap);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public List<WriterDescription> GetAllAvailableServers()
        {
            _lock.EnterReadLock();
            var ret = new List<WriterDescription>(_servers.Where(x=>x.IsAvailable));            
            _lock.ExitReadLock();
            return ret;
        }

        public List<HashMapRecord> GetAllServersForCollector()
        {
            _lock.EnterReadLock();
            var ret = _map.Map.Select(x => x.Clone()).ToList();
            _lock.ExitReadLock();

            ret.ForEach(x => x.Prepare(HashFileType.Collector));

            return ret;
        }

        public List<HashMapRecord> GetHashMap()
        {
            _lock.EnterReadLock();
            var ret = _map.Map.Select(x => x.Clone()).ToList();
            _lock.ExitReadLock();            

            return ret;
        }

        public List<ServerId> GetAllServers2()
        {
            _lock.EnterReadLock();
            var ret = new List<ServerId>(_servers);
            _lock.ExitReadLock();
            return ret;
        }

        public List<ServerId> GetUnavailableServers()
        {
            var ret = new List<ServerId>();
            _lock.EnterReadLock();
            ret = _map.GetUnavailableMap();
            _lock.ExitReadLock();
            return ret;
        }        
        
        public bool IsSomethingHappendInSystem()
        {
            bool ret = false;
            _lock.EnterReadLock();
            
            ret = _servers.Exists(x => !x.IsAvailable || !x.IsServerRestored);

            _lock.ExitReadLock();
            return ret;
        }
    }
}
