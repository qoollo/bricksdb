using System;
using System.Collections.Generic;
using System.Threading;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Tests.NetMock
{
    internal class NetMock : INetMock
    {
        private readonly List<ReceiveWrapperBase> _receivers;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public NetMock()
        {
            _receivers = new List<ReceiveWrapperBase>();
        }

        public void AddServer<TReceive>(ServerId serverId, MockReceive<TReceive> receiveApi)
        {
            _lock.EnterWriteLock();
            try
            {
                _receivers.Add(new ReceiveWrapper<TReceive>(serverId, receiveApi));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveServer<TReceive>(ServerId serverId)
        {
            _lock.EnterWriteLock();
            try
            {
                _receivers.RemoveAll(r => Equals(r.ServerId, serverId));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryConnectClient<TConnection>(ServerId serverId, out TConnection host)
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var receiver in _receivers)
                {
                    if (Equals(receiver.ServerId, serverId) && receiver.Type == typeof (TConnection))
                    {
                        host = ((ReceiveWrapper<TConnection>) receiver).ReceiveApi.Server;
                        return true;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            host = default(TConnection);
            return false;
        }

        private class ReceiveWrapperBase
        {
            public readonly ServerId ServerId;
            public readonly Type Type;

            protected ReceiveWrapperBase(ServerId serverId, Type type)
            {
                ServerId = serverId;
                Type = type;
            }
        }

        private class ReceiveWrapper<TReceive> : ReceiveWrapperBase
        {
            public ReceiveWrapper(ServerId serverId, MockReceive<TReceive> receiveApi)
                :base(serverId, typeof(TReceive))
            {
                ReceiveApi = receiveApi;
            }

            public readonly MockReceive<TReceive> ReceiveApi;
        }
    }
}