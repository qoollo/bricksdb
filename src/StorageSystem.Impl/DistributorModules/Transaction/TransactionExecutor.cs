using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.DistributorModules.Transaction
{
    internal class TransactionExecutor :IDisposable
    {
        private readonly INetModule _net;
        private readonly List<Task> _tasks;
        private readonly object _obj = new object();
        private readonly GlobalQueueInner _queue;

        public TransactionExecutor(INetModule net, int countReplics)
        {
            Contract.Requires(net!=null);
            _net = net;
            _tasks = new List<Task>();

            _queue = GlobalQueue.Queue;

            for (int i = 0; i < countReplics; i++)
            {
                _tasks.Add(new Task(() => { }));
                _tasks[i].Start();
            }
        }

        public void Commit(InnerData data)
        {
            if (data.Transaction.Destination.Count == 1)
                CommitSingleServer(data.Transaction.Destination.First(), data);
            else
            {
                //for (int i = 0; i < data.Transaction.Destination.Count; i++)
                //{
                //    int i1 = i;
                //    _tasks[i].ContinueWith(e => CommitSingleServer(data.Transaction.Destination[i1], data));
                //}
                var list = data.Transaction.Destination.Select(
                        (server, i) => _tasks[i].ContinueWith(e => CommitSingleServer(server, data)));

                Task.WaitAll(list.ToArray());
            }

            if (data.Transaction.IsError)
                Rollback(data, data.Transaction.Destination);
        }

        private void CommitSingleServer(ServerId server, InnerData data)
        {
            var result = _net.Process(server, data);
            if (result.IsError)
            {
                lock (_obj)
                {
                    data.Transaction.SetError();
                    data.Transaction.AddErrorDescription(result.Description);

                    //_queue.DistributorTransactionCallbackQueue.Add(data.Transaction);
                }
            }
        }

        private void Rollback(InnerData data, IEnumerable<ServerId> dest)
        {
            try
            {
                foreach (var server in dest)
                {
                    _net.Rollback(server, data);
                }
            }
            catch (Exception e)
            {
            }
        }

        #region Read

        public InnerData ReadSimple(InnerData data)
        {
            var result = data.Transaction.Destination.Count == 1
                ? _net.ReadOperation(data.Transaction.Destination.First(), data)
                : ReadListServers(data);

            return result;
        }

        private InnerData ReadListServers(InnerData data)
        {
            var retList = new List<InnerData>();
            var obj = new object();

            var list = data.Transaction.Destination.Select((server, i) => _tasks[i].ContinueWith((e) =>
            {
                var ret = _net.ReadOperation(server, data);
                if (ret != null)
                    lock (obj)
                    {
                        retList.Add(ret);
                    }
            }));

            Task.WaitAll(list.ToArray());

            return GetData(retList);
        }

        private InnerData GetData(List<InnerData> list)
        {
            var data = list.Where(x => x != null && x.Data != null);
            if (!data.Any())
            {
                if (list.Count != 0)
                    return list.First();

                return null;
            }

            return data.First();
        }

        #endregion        
        
        public void Dispose()
        {
            _tasks.ForEach(x=>x.Dispose());
        }
    }
}
