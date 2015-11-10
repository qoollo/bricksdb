﻿using System;
using System.Collections.Generic;
using Qoollo.Client.Request;

namespace Qoollo.Client.WriterGate
{
    internal class WriterHandlerEmpty : IWriterApi
    {
        public void UpdateModel()
        {
            throw new Exception(new RequestDescription().ErrorDescription);
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

        public RequestDescription GetAllState()
        {
            return new RequestDescription();
        }

        public RequestDescription InitDb()
        {
            return new RequestDescription();
        }

        public RequestDescription InitDb(string name)
        {
            return new RequestDescription();
        }

        public void DisableDelete()
        {
            throw new Exception(new RequestDescription().ErrorDescription);
        }

        public void EnableDelete()
        {
            throw new Exception(new RequestDescription().ErrorDescription);
        }

        public void StartDelete()
        {
            throw new Exception(new RequestDescription().ErrorDescription);
        }

        public RequestDescription AddDbModule(DbFactory factory)
        {
            return new RequestDescription();
        }
    }
}
