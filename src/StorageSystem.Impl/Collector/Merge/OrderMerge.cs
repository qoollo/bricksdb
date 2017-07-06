using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Collector.Comparer;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;

namespace Qoollo.Impl.Collector.Merge
{
    internal class OrderMerge : MergeBase
    {
        private Qoollo.Logger.Logger _logger = Qoollo.Logger.LoggerStatic.GetThisClassLogger();

        private readonly ScriptParser _scriptParser;
        private readonly CollectorModel _serversModel;

        public OrderMerge(StandardKernel kernel, IDataLoader dataLoader, ScriptParser scriptParser, CollectorModel serversModel)
            : base(kernel, dataLoader)
        {
            _scriptParser = scriptParser;
            _serversModel = serversModel;
        }

        protected override List<SearchData> MergeOrder(OrderSelectTask orderSelectTask,
            List<SingleServerSearchTask> searchTasks, OrderType orderType)
        {
            if (orderSelectTask.ScriptDescription.ContainsCalculatedField)
                return MergeOrderWithCalculated(orderSelectTask, searchTasks, orderType);

            var ret = new List<SearchData>();
                        
            searchTasks.RemoveAll(x => !x.IsServersAvailbale);
            PreLoadPages(searchTasks);

            int viewLength = orderSelectTask.UserPage / 2;

            _logger.Debug("Start load data");
            while (ret.Count < orderSelectTask.UserPage && !IsFinishMerge(searchTasks))
            {
                var current = GetCurrent(orderSelectTask, searchTasks, orderType);

                if (ret.Count == 0 ||
                    DataComparer.Compare(current.GetData(), ret[ret.Count - 1], orderSelectTask.ScriptDescription) != 0)
                    ret.Add(current.GetData());

                current.IncrementPosition();

                ReadSameValue(orderSelectTask, searchTasks, ret[ret.Count - 1]);

                if (searchTasks.Exists(searchTask => searchTask.Length < viewLength && !searchTask.IsAllDataRead))
                {
                    LoadPagesAsync(searchTasks);
                    searchTasks.RemoveAll(x => !x.IsServersAvailbale);
                    _logger.DebugFormat("Load pages. Servers remain = {0}", searchTasks.Count);
                }
            }
            _logger.DebugFormat("Merge data. Count = {0}", ret.Count);
            return ret;
        }

        private List<SearchData> MergeOrderWithCalculated(OrderSelectTask orderSelectTask,
            List<SingleServerSearchTask> searchTasks, OrderType orderType)
        {
            var ret = new List<SearchData>();
            var keys = new List<FieldDescription> {orderSelectTask.ScriptDescription};

            var server = searchTasks.FirstOrDefault();
            if (server == null)
                return ret;
                        
            var keysDescriptions = _scriptParser.GetOrderKeysInner(server.Script);
            if (keysDescriptions.Count != 1)
            {
                searchTasks.ForEach(x => x.OrderKeyDescriptions = keysDescriptions);
                keys = new List<FieldDescription>(keysDescriptions);
            }

            searchTasks.RemoveAll(x => !x.IsServersAvailbale);
            FilterServers(orderSelectTask, searchTasks);
            PreLoadPages(searchTasks);

            int viewLength = orderSelectTask.UserPage / 2;

            _logger.Debug("Start load data");
            while (ret.Count < orderSelectTask.UserPage && !IsFinishMerge(searchTasks))
            {
                var current = GetCurrent(keys, searchTasks, orderType);

                if (current == null)
                    break;

                if (ret.Count == 0 ||
                    DataComparer.Compare(current.GetData(), ret[ret.Count - 1], keys) != 0)
                    ret.Add(current.GetData());

                current.IncrementPosition();

                ReadSameValue(searchTasks, ret[ret.Count - 1], keys);

                if (searchTasks.Exists(searchTask => searchTask.Length < viewLength && !searchTask.IsAllDataRead))
                {
                    LoadPagesAsync(searchTasks);
                    searchTasks.RemoveAll(x => !x.IsServersAvailbale);
                    _logger.DebugFormat("Load pages. Servers remain = {0}", searchTasks.Count);
                }
            }
            _logger.DebugFormat("Merge data. Count = {0}", ret.Count);
            return ret;
        }

        private void ReadSameValue(List<SingleServerSearchTask> searchTasks, SearchData searchData, List<FieldDescription> keys)
        {
            foreach (var searchTask in searchTasks)
            {
                for (int i = 0; i < searchTask.Length; i++)
                {
                    if (DataComparer.Compare(searchTask.GetData(i), searchData, keys) == 0)
                    {
                        searchTask.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        private void ReadSameValue(OrderSelectTask orderSelectTask, List<SingleServerSearchTask> searchTasks, SearchData searchData)
        {
            ReadSameValue(searchTasks, searchData, new List<FieldDescription> { orderSelectTask.ScriptDescription });
        }

        private void PreLoadPages(List<SingleServerSearchTask> searchTasks)
        {
            foreach (var searchTask in searchTasks)
            {
                if (searchTask.Length == 0)
                    LoadPage(searchTask);
            }
        }

        private void LoadPagesAsync(List<SingleServerSearchTask> searchTasks)
        {
            var tasks = new List<Task>();

            foreach (var searchTask in searchTasks.Where(searchTask => !searchTask.IsAllDataRead))
            {
                searchTask.FindNextLastKey();

                var task = searchTask;
                tasks.Add(Task.Factory.StartNew(() => LoadPage(task)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private bool IsFinishMerge(List<SingleServerSearchTask> searchTasks)
        {
            return !searchTasks.Exists(x => !(x.IsAllDataRead && x.Length == 0 || !x.IsServersAvailbale));
        }

        private SingleServerSearchTask GetCurrent(OrderSelectTask orderSelectTask,
            List<SingleServerSearchTask> searchTasks, OrderType orderType)
        {
            return GetCurrent(new List<FieldDescription> {orderSelectTask.ScriptDescription}, searchTasks, orderType);
        }

        private SingleServerSearchTask GetCurrent(List<FieldDescription> keys,
            List<SingleServerSearchTask> searchTasks, OrderType orderType)
        {
            SingleServerSearchTask current = searchTasks.FirstOrDefault(x => !(x.IsAllDataRead && x.Length == 0));

            if (current == null)
                return null;

            foreach (var searchTask in searchTasks)
            {
                if (searchTask.IsAllDataRead && searchTask.Length == 0)
                    continue;

                if (orderType == OrderType.Desc &&
                    DataComparer.Compare(current.GetData(), searchTask.GetData(), keys) < 0)
                    current = searchTask;
                if (orderType == OrderType.Asc &&
                    DataComparer.Compare(current.GetData(), searchTask.GetData(), keys) > 0)
                    current = searchTask;
            }

            return current;
        }

        private void FilterServers(OrderSelectTask orderSelectTask, List<SingleServerSearchTask> searchTasks)
        {
            if (!orderSelectTask.ScriptDescription.ContainsCalculatedField)
                return;

            foreach (var serverSearchTask in searchTasks)
            {
                if (_serversModel.CheckAliveServersWithStep(serverSearchTask.ServerId))
                {
                    var servers = _serversModel.GetAliveServersWithStep(serverSearchTask.ServerId);
                    searchTasks.RemoveAll(x => !servers.Contains(x.ServerId));
                }
            }            
        }
    }
}
