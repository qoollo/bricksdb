using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Background;
using Qoollo.Impl.Collector.Distributor;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Merge;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector
{
    internal class SearchTaskModule : ControlModule
    {
        private string _tableName;
        private MergeBase _mergeBase;
        private DistributorModule _distributor;
        private IDataLoader _dataLoader;
        private BackgroundModule _backgroundModule;
        private ScriptParser _scriptParser;

        public SearchTaskModule(string tableName, MergeBase mergeBase, IDataLoader dataLoader, DistributorModule distributor,
            BackgroundModule backgroundModule, ScriptParser scriptParser)
        {
            _tableName = tableName;
            _mergeBase = mergeBase;
            _dataLoader = dataLoader;
            _distributor = distributor;
            _backgroundModule = backgroundModule;
            _scriptParser = scriptParser;
        }

        public SelectReader CreateReader(string query)
        {
            return CreateReader(query, -1, Consts.UserPage, new List<FieldDescription>());
        }

        public SelectReader CreateReader(string query, int limitCount)
        {
            return CreateReader(query, limitCount, Consts.UserPage, new List<FieldDescription>());
        }

        public SelectReader CreateReader(string query, int limitCount, int userPage)
        {
            return CreateReader(query, limitCount, userPage, new List<FieldDescription>());
        }

        public SelectReader CreateReader(string query, List<FieldDescription> userParametrs)
        {
            return CreateReader(query, -1, Consts.UserPage, userParametrs);
        }

        public SelectReader CreateReader(string query, int limitCount, List<FieldDescription> userParametrs)
        {
            return CreateReader(query, limitCount, Consts.UserPage, userParametrs);
        }

        public SelectReader CreateReader(string query, int limitCount, int userPage,
            List<FieldDescription> userParametrs)
        {
            var type = _scriptParser.ParseQueryType(query);
            switch (type)
            {
                case ScriptType.OrderAsc:
                case ScriptType.OrderDesc:
                    return CreateOrderReader(query, limitCount, userPage, type, userParametrs);
            }

            throw new Exception(Errors.CannotParseQuery);
        }

        private SelectReader CreateOrderReader(string query, int limitCount, int userPage,
            ScriptType type, List<FieldDescription> userParameters)
        {
            var function = _mergeBase.GetMergeFunction(type);

            if (function == null)
            {
                throw new Exception(Errors.CannotParseQuery);
            }

            var description = _scriptParser.PrepareOrderScriptInner(query, _dataLoader.SystemPage + 2);

            description.Item1.PageSize = userPage;
            description.Item1.IsFirstAsk = true;

            if (description == null)
            {
                throw new Exception(Errors.CannotParseQuery);
            }

            var servers = _distributor.GetAvailableServers();
            var searchTask = new OrderSelectTask(servers, description.Item1, description.Item1, description.Item2,
                limitCount, userPage, userParameters, _tableName);

            _scriptParser.PrepareStartPages(searchTask.SearchTasks);

            _dataLoader.LoadAllPagesParallel(searchTask.SearchTasks);
            searchTask.ClearServers();

            description.Item1.IsFirstAsk = false;

            var result = function(searchTask, searchTask.SearchTasks);

            searchTask.CalculateCanRead();

            if ((result.Count < limitCount || limitCount == -1) && searchTask.IsCanRead())
            {
                _backgroundModule.Run(searchTask,
                    () => searchTask.BackgroundLoadInner(_distributor.GetState, _dataLoader, function));
            }
            else
                searchTask.SetFinish();

            var reader = new SelectReader(searchTask, result, limitCount);
            return reader;
        }
    }
}
