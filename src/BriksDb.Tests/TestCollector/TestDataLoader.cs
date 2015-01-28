using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Tests.TestCollector
{
    class TestDataLoader : IDataLoader
    {
        private readonly int _pageSize;
        public Dictionary<ServerId, List<SearchData>> Data;

        public TestDataLoader(int pageSize)
        {
            _pageSize = pageSize;
            Data = new Dictionary<ServerId, List<SearchData>>();
        }

        public void LoadAllPagesParallel(List<SingleServerSearchTask> list)
        {
            foreach (var searchTask in list)
            {
                LoadPage(searchTask);
            }
        }

        public void LoadPage(SingleServerSearchTask searchTask)
        {
            var ret = new List<SearchData>(Data[searchTask.ServerId]);

            if (Data[searchTask.ServerId].Count == 0)
                searchTask.AllDataRead();

            if (_pageSize < ret.Count)
                ret.RemoveRange(_pageSize, ret.Count - _pageSize);

            if (_pageSize < Data[searchTask.ServerId].Count)
                Data[searchTask.ServerId].RemoveRange(0, _pageSize);
            else
                Data[searchTask.ServerId].RemoveRange(0, Data[searchTask.ServerId].Count);

            searchTask.AddPage(ret);
        }

        public int SystemPage { get { return _pageSize; } }
    }
}
