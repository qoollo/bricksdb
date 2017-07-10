using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Collector.CollectorNet;
using Qoollo.Impl.Collector.Interfaces;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector.Load
{
    internal class DataLoader : ControlModule, IDataLoader
    {
        private readonly CollectorNetModule _net;
        private readonly int _serverPageSize;
        private readonly BackgroundModule _backgroundModule;
        public int SystemPage { get { return _serverPageSize; } }

        public DataLoader(StandardKernel kernel, CollectorNetModule net, int serverPageSize, BackgroundModule backgroundModule)
            :base(kernel)
        {
            _net = net;
            _serverPageSize = serverPageSize;
            _backgroundModule = backgroundModule;
        }

        public void LoadAllPagesParallel(List<SingleServerSearchTask> list)
        {
            var tasks = list.Select(searchTask =>
                _backgroundModule.RunAsTask(() =>
                    LoadSingleServer(searchTask))).ToArray();

            Task.WaitAll(tasks);
        }

        public void LoadPage(SingleServerSearchTask searchTask)
        {
            LoadSingleServer(searchTask);
        }

        private void LoadSingleServer(SingleServerSearchTask searchTask)
        {
            var description = new SelectDescription(searchTask.IdDescription, searchTask.Script,
                _serverPageSize, searchTask.UserParametrs)
            {
                TableName = searchTask.TableName,
                UseUserScript = searchTask.IsUserScript,
                OrderKeyDescriptions = searchTask.OrderKeyDescriptions
            };
            var result = _net.SelectQuery(searchTask.ServerId, description);

            if (result.Item1.IsError)
            {
                searchTask.ServerUnavailable();
            }
            else
            {
                if (result.Item2.IsAllDataRead)
                    searchTask.AllDataRead();
                searchTask.AddPage(result.Item2.Data);
            }
        }
    }
}
