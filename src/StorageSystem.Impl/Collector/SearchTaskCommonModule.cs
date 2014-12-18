using System.Collections.Generic;
using Qoollo.Impl.Collector.Background;
using Qoollo.Impl.Collector.Distributor;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Merge;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector
{
    internal class SearchTaskCommonModule:ControlModule
    {
        private Dictionary<string, SearchTaskModule> _apis; 
        private DistributorModule _distributor;
        private IDataLoader _dataLoader;
        private BackgroundModule _backgroundModule;

        public SearchTaskCommonModule(IDataLoader dataLoader, DistributorModule distributor,
            BackgroundModule backgroundModule)
        {
            _dataLoader = dataLoader;
            _distributor = distributor;
            _backgroundModule = backgroundModule;
            _apis = new Dictionary<string, SearchTaskModule>();
        }

        public SearchTaskModule CreateApi(string tableName, ScriptParser scriptParser)
        {
            if (_apis.ContainsKey(tableName))
                return null;

            var merge = new OrderMerge(_dataLoader, scriptParser);

            var api = new SearchTaskModule(tableName, merge, _dataLoader, _distributor, 
                _backgroundModule, scriptParser);

            _apis.Add(tableName, api);

            return api;
        }
    }
}
