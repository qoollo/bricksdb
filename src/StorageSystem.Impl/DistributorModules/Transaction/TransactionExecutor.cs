using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;

namespace Qoollo.Impl.DistributorModules.Transaction
{
    internal class TransactionExecutor :IDisposable
    {
        private INetModule _net;
        private List<Task> _tasks;
        private object _lock = new object();

        public TransactionExecutor(INetModule net, int countReplics)
        {
            _net = net;
            _tasks = new List<Task>();
            for (int i = 0; i < countReplics; i++)
            {
                _tasks.Add(new Task(() => { }));
                _tasks[i].Start();
            }
        }

        public void Commit(InnerData data)
        {
            var destination = new List<ServerId>(data.Transaction.Destination);
            if (data.Transaction.Destination.Count == 1)
            {
                var result = _net.Process(data.Transaction.Destination.First(), data);

                if (result.IsError)
                {
                    data.Transaction.SetError();
                    data.Transaction.AddErrorDescription(result.Description);
                }
            }
            else
            {
                var list = data.Transaction.Destination.Select((server, i) => _tasks[i].ContinueWith((e) =>
                    {
                        var result = _net.Process(server, data);

                        if (result.IsError)
                        {
                            lock (_lock)
                            {
                                data.Transaction.SetError();
                                data.Transaction.AddErrorDescription(result.Description);
                            }
                        }
                    }));

                Task.WaitAll(list.ToArray());
            }

            if (data.Transaction.IsError)
                Rollback(data, destination);
        }

        public InnerData ReadSimple(InnerData data)
        {
            InnerData result = null;

            if (data.Transaction.Destination.Count == 1)
            {
                result = _net.ReadOperation(data.Transaction.Destination.First(), data);
            }
            else
            {
                var retList = new List<InnerData>();

                var list = data.Transaction.Destination.Select((server, i) => _tasks[i].ContinueWith((e) =>
                {
                    var ret = _net.ReadOperation(server, data);
                    if (ret != null)
                        lock (_lock)
                        {
                            retList.Add(ret);
                        }
                }));

                Task.WaitAll(list.ToArray());

                result = GetData(retList);
            }

            return result;
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

        private void Rollback(InnerData data, List<ServerId> dest )
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
        
        public void Dispose()
        {
            _tasks.ForEach(x=>x.Dispose());
        }
    }
}
