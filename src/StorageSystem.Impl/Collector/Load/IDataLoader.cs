using System.Collections.Generic;
using Qoollo.Impl.Collector.Tasks;

namespace Qoollo.Impl.Collector.Load
{
    internal interface IDataLoader
    {
        void LoadAllPagesParallel(List<SingleServerSearchTask> list);

        void LoadPage(SingleServerSearchTask searchTask);

        int SystemPage { get; }
    }
}
