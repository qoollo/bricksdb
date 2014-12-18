using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Proxy.Model
{
    internal class DistributorSystemModel:IDisposable
    {
        private List<DistributorDescription> _model;
        private ReaderWriterLockSlim _lock;

        public DistributorSystemModel()
        {
            _model = new List<DistributorDescription>();
            _lock = new ReaderWriterLockSlim();
        }

        #region public

        public void AddServer(ServerId server)
        {
            _lock.EnterWriteLock();
            if (!_model.Exists(x => x.Equals(server)))
            {
                var hash = HashConvertor.GetString(server.ToString());
                _model.Add(new DistributorDescription(hash, server));
            }
            _lock.ExitWriteLock();
        }

        public void AddServers(List<ServerId> servers)
        {
            _lock.EnterWriteLock();
            foreach (var server in servers)
            {
                if (!_model.Exists(x => x.Equals(server)))
                {
                    var hash = HashConvertor.GetString(server.ToString());
                    _model.Add(new DistributorDescription(hash, server));
                }    
            }            
            _lock.ExitWriteLock();
        }

        public Transaction CreateTransaction(string hash)
        {
            var server = GetDestinationServer();
            if (server == null)
            {
                var tr = new Transaction("", "");
                tr.SetError();
                tr.AddErrorDescription(Errors.NotAvailableServersInSystem);
                return tr;
            }            

            return new Transaction(hash, server.Hash);
        }

        public DistributorDescription GetDestination(UserTransaction transaction)
        {
            DistributorDescription ret = null;
            _lock.EnterReadLock();
            if (_model.Count > 0)
            {
                ret = _model.FirstOrDefault(x => x.Hash == transaction.DistributorHash);
            }
            _lock.ExitReadLock();
            return ret;
        }

        public ServerId GetNewDestination()
        {
            return GetDestinationServer();
        }     

        public List<ServerId> GetDistributorsList()
        {
            List<ServerId> list = null;
            _lock.EnterReadLock();
            list = new List<ServerId>(_model);
            _lock.ExitReadLock();
            return list;
        }
       
        public List<DistributorDescription> GetDistributorsListForProxy()
        {
            List<DistributorDescription> list = null;
            _lock.EnterReadLock();
            list = new List<DistributorDescription>(_model);
            _lock.ExitReadLock();
            return list;
        }

        #endregion

        #region Available

        public void ServerNotAvailable(ServerId serverId)
        {
            _lock.EnterWriteLock();

            var server = _model.FirstOrDefault(x => x.Equals(serverId));

            if (server == null)
            {
                //TODO непонятный косяк и непонятно что с ним делать, кроме как логировать
            }
            else
            {
                server.NotAvailable();
            }

            _lock.ExitWriteLock();
        }

        public void ServerAvailable(ServerId serverId)
        {
            _lock.EnterWriteLock();

            var server = _model.FirstOrDefault(x => x.Equals(serverId));

            if (server == null)
            {
                //TODO непонятный косяк и непонятно что с ним делать, кроме как логировать
            }
            else
            {
                server.Available();
            }

            _lock.ExitWriteLock();
        }                

        #endregion

        #region private

        private DistributorDescription GetDestinationServer()
        {
            DistributorDescription ret = null;
            _lock.EnterReadLock();
            var list = _model.Where(x => x.IsAvailable);
            if (list.Any())
            {
                var ll = list.OrderBy((x) => x.Load);

                ret = ll.First();

                if (ret != null) ret.UpdateLoad(ret.Load + 1);
            }
            _lock.ExitReadLock();
            return ret;
        }

        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
