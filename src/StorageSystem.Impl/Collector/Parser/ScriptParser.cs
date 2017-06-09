﻿using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.Support;

namespace Qoollo.Impl.Collector.Parser
{
    public abstract class ScriptParser
    {
        private IUserCommandsHandler _commandsHandler;

        public void SetCommandsHandler(IUserCommandsHandler handler)
        {
            _commandsHandler = handler;
        }

        public abstract ScriptType ParseQueryType(string script);

        public abstract Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler);
        
        public abstract Tuple<FieldDescription, string> PrepareKeyScript(string script, IUserCommandsHandler handler);
                
        public Tuple<FieldDescription, string> PrepareOrderScriptInner(string script, int pageSize)
        {            
            return PrepareOrderScript(script, pageSize, _commandsHandler);
        }

        public Tuple<FieldDescription, string> PrepareKeyScriptInner(string script)
        {
            return PrepareKeyScript(script, _commandsHandler);
        }

        public void PrepareStartPages(List<SingleServerSearchTask> searchTasks)
        {
            foreach (var searchTask in searchTasks)
            {
                object value = searchTask.IdDescription.SystemFieldType.IsValueType
                    ? Activator.CreateInstance(searchTask.IdDescription.SystemFieldType)
                    : null;

                searchTask.IdDescription.Value = value;

                //TODO check
                if (searchTask.OrderKeyDescriptions != null)
                foreach (var keyDescription in searchTask.OrderKeyDescriptions)
                {
                        value = keyDescription.SystemFieldType.IsValueType
                            ? Activator.CreateInstance(keyDescription.SystemFieldType)
                            : null;
                    keyDescription.Value = value;
                }
            }
        }

        public List<FieldDescription> GetOrderKeysInner(string script)
        {
            return GetOrderKeys(script, _commandsHandler);
        }

        public abstract List<FieldDescription> GetOrderKeys(string script, IUserCommandsHandler handler);
    }
}
