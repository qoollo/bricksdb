using System.Collections.Generic;
using Qoollo.Impl.Collector.Background;
using Qoollo.Impl.Collector.Distributor;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Merge;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector
{
    internal class SearchTaskCommonModule:ControlModule
    {
        private readonly Dictionary<string, SearchTaskModule> _apis; 
        private readonly DistributorModule _distributor;
        private readonly IDataLoader _dataLoader;
        private readonly BackgroundModule _backgroundModule;
        private readonly CollectorModel _serversModel;

        public SearchTaskCommonModule(IDataLoader dataLoader, DistributorModule distributor,
            BackgroundModule backgroundModule, CollectorModel serversModel)
        {
            _dataLoader = dataLoader;
            _distributor = distributor;
            _backgroundModule = backgroundModule;
            _serversModel = serversModel;
            _apis = new Dictionary<string, SearchTaskModule>();
        }

        public SearchTaskModule CreateApi(string tableName, ScriptParser scriptParser)
        {
            if (_apis.ContainsKey(tableName))
                return null;

            var merge = new OrderMerge(_dataLoader, scriptParser, _serversModel);

            var api = new SearchTaskModule(tableName, merge, _dataLoader, _distributor, 
                _backgroundModule, scriptParser);

            _apis.Add(tableName, api);

            return api;
        }
    }
}
