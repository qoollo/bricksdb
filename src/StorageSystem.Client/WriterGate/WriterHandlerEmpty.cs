using System;
using System.Collections.Generic;
using Qoollo.Client.Request;

namespace Qoollo.Client.WriterGate
{
    internal class WriterHandlerEmpty : IWriterApi
    {
        public RequestDescription UpdateModel()
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(bool isModelUpdated)
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated)
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(bool isModelUpdated, string tableName)
        {
            return new RequestDescription();
        }

        public RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated, string tableName)
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

        public RequestDescription AddDbModule(DbFactory factory)
        {
            return new RequestDescription();
        }
    }
}
