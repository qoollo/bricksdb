using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Timestamps;
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
            if (data.DistributorData.Destination.Count == 1)
                CommitSingleServer(data.DistributorData.Destination.First(), data);
            else
            {
                for (int i = 0; i < data.DistributorData.Destination.Count; i++)
                {
                    int i1 = i;
                    _tasks[i].ContinueWith(e => CommitSingleServer(data.DistributorData.Destination[i1], data));
                }
            }
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

                    _queue.TransactionQueue.Add(data.Transaction);
                }
            }
        }

        #region Read

        public InnerData ReadSimple(InnerData data)
        {
            var result = data.DistributorData.Destination.Count == 1
                ? _net.ReadOperation(data.DistributorData.Destination.First(), data)
                : ReadListServers(data);
            
            return result;
        }

        private InnerData ReadListServers(InnerData data)
        {
            var retList = new List<InnerData>();
            var obj = new object();

            var list = data.DistributorData.Destination.Select((server, i) => _tasks[i].ContinueWith((e) =>
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
