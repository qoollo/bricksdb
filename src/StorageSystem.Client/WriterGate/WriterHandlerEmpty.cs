using System;
using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Client.WriterGate
{
    internal class WriterHandlerEmpty : IWriterApi
    {
        public RequestDescription UpdateModel()
        {
            return new RequestDescription();
        }

        public RequestDescription Restore()
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(RestoreMode mode)
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(List<ServerAddress> servers, RestoreMode mode)
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(RestoreMode mode, string tableName)
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(List<ServerAddress> servers, RestoreMode mode, string tableName)
        {
            return new RequestDescription();
        }

        public bool IsRestoreCompleted()
        {
            throw new Exception(new RequestDescription().ErrorDescription);
        }

        public List<ServerAddress> FailedServers()
        {
            throw new Exception(new RequestDescription().ErrorDescription);
        }

        public string GetAllState()
        {
            return new RequestDescription().ErrorDescription;
        }

        public RequestDescription InitDb()
        {
            return new RequestDescription();
        }

        public RequestDescription InitDb(string name)
        {
            return new RequestDescription();
        }

        public RequestDescription DisableDelete()
        {
            return new RequestDescription();
        }

        public RequestDescription EnableDelete()
        {
            return new RequestDescription();
        }

        public RequestDescription StartDelete()
        {
            return new RequestDescription();
        }

        public RequestDescription RunDelete()
        {
            return new RequestDescription();
        }

        public RequestDescription AddDbModule(DbFactory factory)
        {
            return new RequestDescription();
        }
    }
}
