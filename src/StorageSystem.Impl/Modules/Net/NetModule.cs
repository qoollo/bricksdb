using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.NetInterfaces;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class NetModule : ControlModule
    {
        private Dictionary<ServerId, ISingleConnection> _servers;
        protected ReaderWriterLockSlim _lock;
        private ConnectionConfiguration _configuration;
        private ConnectionTimeoutConfiguration _connectionTimeout;

        protected NetModule(ConnectionConfiguration connectionConfiguration, ConnectionTimeoutConfiguration connectionTimeout)
        {
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(connectionTimeout != null);
            _configuration = connectionConfiguration;
            _connectionTimeout = connectionTimeout;
            _lock = new ReaderWriterLockSlim();
            _servers = new Dictionary<ServerId, ISingleConnection>();
        }

        #region Find servers

        protected ISingleConnection FindServer(ServerId server)
        {
            _lock.EnterReadLock();
            var value = _servers.FirstOrDefault(x => x.Key.Equals(server));
            _lock.ExitReadLock();
            if (!value.Equals(default(KeyValuePair<ServerId, ControlModule>)))
                return value.Value;
            else
                return null;
        }

        private ISingleConnection FindServerNoLock(ServerId server)
        {
            var value = _servers.FirstOrDefault(x => x.Key.Equals(server));
            if (!value.Equals(default(KeyValuePair<ServerId, ControlModule>)))
                return value.Value;
            else
                return null;
        }

        protected ISingleConnection FindServer<T>()
        {
            _lock.EnterReadLock();
            var value = _servers.FirstOrDefault(x => x.Value is T);
            _lock.ExitReadLock();
            if (!value.Equals(default(KeyValuePair<ServerId, ControlModule>)))
                return value.Value;
            else
                return null;
        }

        #endregion        

        #region Ping

        protected void PingServers(List<ServerId> servers, Action<ServerId> serverAvailable,
            Func<ServerId, ICommonCommunicationNet> findServer, Func<ServerId, bool> connect)
        {
            servers.ForEach(server =>
            {
                bool result = PingServer(server, findServer, connect);

                if (result)
                    serverAvailable(server);
                else
                {
                    RemoveConnection(server);
                    result = PingServer(server, findServer, connect);
                    if (result)
                        serverAvailable(server);
                }
            });
        }       

        public List<ServerId> GetServersByType(Type type)
        {
            _lock.EnterReadLock();
            var servers = _servers.Where(x => x.Value.GetType() == type).Select(x => x.Key).ToList();
            _lock.ExitReadLock();

            return servers;
        }

        private bool PingServer(ServerId server, Func<ServerId, ICommonCommunicationNet> findServer,
            Func<ServerId, bool> connect)
        {
            bool result = connect(server);
            if (result)
            {
                var connection = findServer(server);
                if (connection == null)
                    result = false;
                else
                {
                    var pingRes = connection.Ping();
                    if (pingRes is FailNetResult)
                        result = false;
                }
            }

            return result;
        }

        #endregion

        protected bool ConnectToServer(ServerId server,
            Func<ServerId, ConnectionConfiguration, ConnectionTimeoutConfiguration, ISingleConnection> connectFunc)
        {
            bool ret = true;

            if (FindServer(server) == null)
            {
                var connection = connectFunc(server, _configuration, _connectionTimeout);

                bool result = connection.Connect();

                if (!result)
                {
                    ret = false;
                    connection.Dispose();
                }
                else
                {
                    _lock.EnterWriteLock();

                    if (FindServerNoLock(server) == null)
                        _servers.Add(server, connection);
                    else
                        connection.Dispose();

                    _lock.ExitWriteLock();
                }
            }

            return ret;
        }

        protected void RemoveConnection(ServerId server)
        {
            _lock.EnterWriteLock();

            var s = FindServerNoLock(server);

            _servers.Remove(server);

            if (s != null)
                s.Dispose();

            _lock.ExitWriteLock();
        }
        
        protected override void Dispose(bool isUserCall)
        {
            _lock.EnterWriteLock();

            _servers.Values.ToList().ForEach(x => x.Dispose());

            _lock.ExitWriteLock();

            base.Dispose(isUserCall);
        }
    }
}
