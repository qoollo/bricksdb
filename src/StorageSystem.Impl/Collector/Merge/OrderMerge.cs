using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Collector.Comparer;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;

namespace Qoollo.Impl.Collector.Merge
{
    internal class OrderMerge : MergeBase
    {
        private ScriptParser _scriptParser;

        public OrderMerge(IDataLoader dataLoader, ScriptParser scriptParser)
            : base(dataLoader)
        {
            _scriptParser = scriptParser;
        }

        protected override List<SearchData> MergeOrder(OrderSelectTask orderSelectTask,
            List<SingleServerSearchTask> searchTasks, OrderType orderType)
        {
            var ret = new List<SearchData>();

            searchTasks.RemoveAll(x => !x.IsServersAvailbale);

            PreLoadPages(searchTasks);

            while (ret.Count < orderSelectTask.UserPage && ! IsFinishMerge(searchTasks))
            {
                var current = GetCurrent(orderSelectTask, searchTasks, orderType);

                if (ret.Count == 0 ||
                    DataComparer.Compare(current.GetData(), ret.Last(), orderSelectTask.ScriptDescription) != 0)
                    ret.Add(current.GetData());

                current.IncrementPosition();

                ReadSameValue(orderSelectTask, searchTasks, ret.Last());

                LoadPages(searchTasks);
                searchTasks.RemoveAll(x => !x.IsServersAvailbale);
            }

            return ret;
        }

        private void ReadSameValue(OrderSelectTask orderSelectTask, List<SingleServerSearchTask> searchTasks,
            SearchData searchData)
        {
            foreach (var searchTask in searchTasks)
            {
                if (!searchTask.IsAllDataRead && searchTask.Length > 0 &&
                    DataComparer.Compare(searchTask.GetData(), searchData, orderSelectTask.ScriptDescription) == 0)
                    searchTask.IncrementPosition();
            }
        }

        private void PreLoadPages(List<SingleServerSearchTask> searchTasks)
        {
            foreach (var searchTask in searchTasks)
            {
                if (searchTask.Length == 0)
                    LoadPage(searchTask);
            }
        }

        private void LoadPages(List<SingleServerSearchTask> searchTasks)
        {
            foreach (var searchTask in searchTasks)
            {
                if (searchTask.Length == 0 && !searchTask.IsAllDataRead)
                {
                    searchTask.FindNextLastKey();
                    //searchTask.SetNewScript(_scriptParser.SetNextPage(searchTask.Script, searchTask.LastKey));

                    LoadPage(searchTask);
                }
            }
        }

        private bool IsFinishMerge(List<SingleServerSearchTask> searchTasks)
        {
            return !searchTasks.Exists(x => !(x.IsAllDataRead && x.Length == 0 || !x.IsServersAvailbale));
        }

        private SingleServerSearchTask GetCurrent(OrderSelectTask orderSelectTask,
            List<SingleServerSearchTask> searchTasks, OrderType orderType)
        {
            SingleServerSearchTask current = searchTasks.FirstOrDefault(x => !(x.IsAllDataRead && x.Length == 0));

            if (current == null)
                return null;

            foreach (var searchTask in searchTasks) 
            {
                if (searchTask.IsAllDataRead && searchTask.Length==0)
                    continue;

                if (orderType == OrderType.Desc &&
                    DataComparer.Compare(current.GetData(), searchTask.GetData(), orderSelectTask.ScriptDescription) < 0)
                    current = searchTask;
                if (orderType == OrderType.Asc &&
                    DataComparer.Compare(current.GetData(), searchTask.GetData(), orderSelectTask.ScriptDescription) > 0)
                    current = searchTask;
            }

            return current;
        }
    }
}
