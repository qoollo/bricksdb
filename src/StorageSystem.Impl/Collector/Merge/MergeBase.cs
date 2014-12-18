using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector.Merge
{
    internal abstract class MergeBase:ControlModule
    {
        private readonly IDataLoader _dataLoader;

        protected MergeBase(IDataLoader dataLoader)
        {
            _dataLoader = dataLoader;
        }

        protected void LoadPage(SingleServerSearchTask searchTask)
        {
            _dataLoader.LoadPage(searchTask);
        }

        public Func<OrderSelectTask, List<SingleServerSearchTask>, List<SearchData>> GetMergeFunction(ScriptType type)
        {
            switch (type)
            {
                case ScriptType.OrderAsc:
                    return MergeOrderAsc;
                case ScriptType.OrderDesc:
                    return MergeOrderDesc;
            }

            return null;
        }

        private List<SearchData> MergeOrderAsc(OrderSelectTask orderSelectTask, List<SingleServerSearchTask> searchTasks)
        {
            return MergeOrder(orderSelectTask, searchTasks, OrderType.Asc);
        }

        private List<SearchData> MergeOrderDesc(OrderSelectTask orderSelectTask, List<SingleServerSearchTask> searchTasks)
        {
            return MergeOrder( orderSelectTask,searchTasks, OrderType.Desc);
        }

        protected abstract List<SearchData> MergeOrder(OrderSelectTask orderSelectTask, List<SingleServerSearchTask> searchTasks, OrderType orderType);
    }
}
