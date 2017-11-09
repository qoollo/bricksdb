using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ninject;
using Qoollo.Impl.Collector.Interfaces;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector.Model
{
    internal class CollectorModel : ControlModule,  ICollectorModel
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public bool UseStart { get; protected set; }
        private List<WriterDescription> _servers; 
        private readonly ReaderWriterLockSlim _lock;
        private int _countReplics;
        private readonly HashMap _map;

        public CollectorModel(StandardKernel kernel) :base(kernel)
        {
            _lock = new ReaderWriterLockSlim();
            _servers = new List<WriterDescription>();
            _map = new HashMap(kernel, HashFileType.Collector);
        }

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

        public void StartConfig()
        {
            var config = Kernel.Get<ICommonConfiguration>();
            UseStart = Kernel.Get<ICollectorConfiguration>().UseHashFile;   
            _countReplics = config.CountReplics;

            _map.Start();
        }

        public override void Start()
        {
            _map.CreateMap();
            _servers = _map.Servers;
        }

        public void NewServers(List<Tuple<ServerId, string, string>> servers)
        {
            _lock.EnterWriteLock();
            _map.CreateMapFromDistributor(servers);
            _servers = _map.Servers;
            _lock.ExitWriteLock();
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

        public List<ServerId> GetAvailableServers()
        {
            var ret = new List<ServerId>();
            _lock.EnterReadLock();
            ret = _map.GetAvailableMap();
            _lock.ExitReadLock();
            return ret;
        }

        private List<ServerId> GetNearServers(ServerId server)
        {
            var ret = new List<ServerId>();
            int pos = _map.Map.FindIndex(x => x.ServerId.Equals(server));
            int index = (pos + 1)%_map.Map.Count;

            for (int i = 0; i < _countReplics-1; i++)
            {
                while (pos != index)
                {
                    var s = _map.Map[index].ServerId;
                    if (!ret.Contains(s) && !server.Equals(s))
                    {
                        ret.Add(s);
                        index = ++index%_map.Map.Count;
                        break;
                    }
                    index = ++index%_map.Map.Count;
                }

                if (pos == index && ret.Count != _countReplics)
                    return null;
            }
            return ret;
        }

        public SystemSearchStateInner GetSystemState()
        {
            SystemSearchStateInner ret;

            var unavailableServers = GetUnavailableServers();

            _lock.EnterReadLock();

            if (unavailableServers.Count == 0)
            {
                ret = _countReplics < _servers.Count
                    ? SystemSearchStateInner.AllServersAvailable
                    : SystemSearchStateInner.InvalidSystemConfiguration;
            }
            else
            {
                ret = SystemSearchStateInner.AllDataAvailable;
                foreach (var unavailableServer in unavailableServers)
                {
                    var servers = GetNearServers(unavailableServer);
                    if (servers == null)
                    {
                        ret= SystemSearchStateInner.InvalidSystemConfiguration;
                        break;
                    }

                    bool fail = servers.All(unavailableServers.Contains);

                    if (fail)
                    {
                        ret= SystemSearchStateInner.SomeDataUnavailable;
                        break;
                    }
                }                
            }

            _lock.ExitReadLock();

            return ret;
        }

        public bool CheckAliveServersWithStep(ServerId startServer)
        {
            _lock.EnterReadLock();
            try
            {
                var index = _servers.FindIndex(startServer.Equals);
                if (index == -1)
                    return false;

                var count = _servers.Count/ _countReplics;
                for (int i = 0; i < count; i++)
                {
                    if (!_servers[index].IsAvailable)
                        return false;
                    index = (index + _countReplics) %_servers.Count;
                }

                return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public List<ServerId> GetAliveServersWithStep(ServerId startServer)
        {
            var ret = new List<ServerId>();
            _lock.EnterReadLock();
            try
            {
                var index = _servers.FindIndex(startServer.Equals);
                if (index == -1)
                    return ret;

                var count = _servers.Count / _countReplics;
                for (int i = 0; i < count; i++)
                {
                    ret.Add(_servers[index]);
                    index = (index + _countReplics) % _servers.Count;
                }

                return ret;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
