using System;
using System.Collections.Generic;
using System.Threading;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.DistributorModules.Model
{
    internal class DistributorSystemModel
    {
        private readonly List<ServerId> _model;
        private readonly ReaderWriterLockSlim _lock;

        public DistributorSystemModel()
        {
            _model = new List<ServerId>();
            _lock = new ReaderWriterLockSlim();
        }

        #region public

        public bool AddServer(ServerId server)
        {
            bool ret = false;
            _lock.EnterWriteLock();
            if (!_model.Exists(x => x.Equals(server)))
            {
                _model.Add(server);
                ret = true;
            }
            _lock.ExitWriteLock();
            return ret;
        }

        public List<ServerId> GetDistributorList()
        {
            _lock.EnterWriteLock();
            var list = new List<ServerId>(_model);
            _lock.ExitWriteLock();
            return list;
        }

        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
