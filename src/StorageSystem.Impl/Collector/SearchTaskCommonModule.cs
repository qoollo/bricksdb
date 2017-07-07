using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Collector.Merge;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector
{
    internal class SearchTaskCommonModule:ControlModule
    {
        private readonly Dictionary<string, SearchTaskModule> _apis; 

        public SearchTaskCommonModule(StandardKernel kernel)
            :base(kernel)
        {
            _apis = new Dictionary<string, SearchTaskModule>();
        }

        public SearchTaskModule CreateApi(string tableName, ScriptParser scriptParser)
        {
            if (_apis.ContainsKey(tableName))
                return null;

            var merge = new OrderMerge(Kernel, scriptParser);
            merge.Start();

            var api = new SearchTaskModule(Kernel, tableName, merge, scriptParser);
            _apis.Add(tableName, api);

            return api;
        }
    }
}
