﻿using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Collector.Interfaces;
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
        private readonly string _tableName;
        private readonly MergeBase _mergeBase;
        private IDistributorModule _distributor;
        private IDataLoader _dataLoader;
        private IBackgroundModule _backgroundModule;
        private readonly ScriptParser _scriptParser;

        public SearchTaskModule(StandardKernel kernel, string tableName, MergeBase mergeBase, ScriptParser scriptParser)
            :base(kernel)
        {
            _tableName = tableName;
            _mergeBase = mergeBase;
            _scriptParser = scriptParser;
        }

        public override void Start()
        {
            _distributor = Kernel.Get<IDistributorModule>();
            _dataLoader = Kernel.Get<IDataLoader>();
            _backgroundModule = Kernel.Get<IBackgroundModule>();
        }

        public SelectReader CreateReader(string query, bool isUserScript = false, FieldDescription field = null)
        {
            return CreateReader(query, -1, Consts.UserPage, new List<FieldDescription>(), isUserScript, field);
        }

        public SelectReader CreateReader(string query, int limitCount, bool isUserScript = false,
            FieldDescription field = null)
        {
            return CreateReader(query, limitCount, Consts.UserPage, new List<FieldDescription>(), isUserScript, field);
        }

        public SelectReader CreateReader(string query, int limitCount, int userPage, bool isUserScript = false,
            FieldDescription field = null)
        {
            return CreateReader(query, limitCount, userPage, new List<FieldDescription>(), isUserScript, field);
        }

        public SelectReader CreateReader(string query, List<FieldDescription> userParametrs, bool isUserScript = false,
            FieldDescription field = null)
        {
            return CreateReader(query, -1, Consts.UserPage, userParametrs, isUserScript, field);
        }

        public SelectReader CreateReader(string query, int limitCount, List<FieldDescription> userParametrs,
            bool isUserScript = false, FieldDescription field = null)
        {
            return CreateReader(query, limitCount, Consts.UserPage, userParametrs, isUserScript, field);
        }

        public SelectReader CreateReader(string query, int limitCount, int userPage,
            List<FieldDescription> userParametrs, bool isUserScript = false, FieldDescription field = null)
        {
            var type = _scriptParser.ParseQueryType(query);
            switch (type)
            {
                case ScriptType.OrderAsc:
                case ScriptType.OrderDesc:
                    return CreateOrderReader(query, limitCount, userPage, type, userParametrs, isUserScript, field);
            }

            throw new Exception(Errors.CannotParseQuery);
        }

        private SelectReader CreateOrderReader(string query, int limitCount, int userPage,
            ScriptType type, List<FieldDescription> userParameters, bool isUserScript, FieldDescription field = null)
        {
            var function = _mergeBase.GetMergeFunction(type);

            if (function == null)
            {
                throw new Exception(Errors.CannotParseQuery);
            }

            var description = CreateDescription(query, userPage, isUserScript, field);

            var servers = _distributor.GetAvailableServers();
            var searchTask = new OrderSelectTask(servers, description.Item1, description.Item1, description.Item2,
                limitCount, userPage, userParameters, _tableName, isUserScript);

            //TODO check
            var keysDescriptions = _scriptParser.GetOrderKeysInner(query);
            if (keysDescriptions.Count != 1)
            {
                searchTask.SearchTasks.ForEach(x => x.OrderKeyDescriptions = keysDescriptions);
            }

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

        private Tuple<FieldDescription, string> CreateDescription(string query, int userPage, bool isUserScript,
            FieldDescription field = null)
        {
            if (!isUserScript)
            {
                var description = _scriptParser.PrepareOrderScriptInner(query, _dataLoader.SystemPage + 2);

                if (description == null)
                {
                    throw new Exception(Errors.CannotParseQuery);
                }

                description.Item1.PageSize = userPage;
                description.Item1.IsFirstAsk = true;

                return description;
            }

            var ret = new Tuple<FieldDescription, string>(field, query);
            ret.Item1.IsFirstAsk = true;
            ret.Item1.PageSize = userPage;

            return ret;
        }
    }
}
