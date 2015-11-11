using System;
using System.Collections.Generic;
using System.Linq;
using Qoollo.Client.Request;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;

namespace Qoollo.Client.WriterGate
{
    internal class WriterHandler: IWriterApi
    {
        private readonly WriterSystem _writer;

        public WriterHandler(WriterSystem writer)
        {
            _writer = writer;
        }

        public RequestDescription UpdateModel()
        {            
            return new RequestDescription(_writer.Distributor.UpdateModel());
        }

        public RequestDescription Restore(bool isModelUpdated)
        {
            string result = _writer.Distributor.Restore(isModelUpdated);
            return new RequestDescription(result);
        }

        public RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated)
        {
            var list = new List<ServerId>();
            servers.ForEach(x => list.Add(new ServerId(x.Host, x.Port)));
            string result = _writer.Distributor.Restore(list, isModelUpdated);
            return new RequestDescription(result);
        }

        public RequestDescription Restore(bool isModelUpdated, string tableName)
        {
            string result = _writer.Distributor.Restore(isModelUpdated, tableName);
            return new RequestDescription(result);
        }

        public RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated, string tableName)
        {
            var list = new List<ServerId>();
            servers.ForEach(x => list.Add(new ServerId(x.Host, x.Port)));
            string result = _writer.Distributor.Restore(list,
                isModelUpdated, tableName);
            return new RequestDescription(result);
        }

        public bool IsRestoreCompleted()
        {
            return _writer.Distributor.IsRestoreCompleted();
        }

        public List<ServerAddress> FailedServers()
        {
            return
                _writer.Distributor.FailedServers().Select(x => new ServerAddress(x.RemoteHost, x.Port)).ToList();
        }

        public string GetAllState()
        {
            return _writer.Distributor.GetAllState();
        }

        public RequestDescription InitDb()
        {
            var result = _writer.DbModule.InitDb();
            return new RequestDescription(result);
        }

        public RequestDescription InitDb(string name)
        {
            var result = _writer.DbModule.InitDb(name);
            return new RequestDescription(result);
        }

        public RequestDescription DisableDelete()
        {
            return new RequestDescription(_writer.Distributor.DisableDelete());
        }

        public RequestDescription EnableDelete()
        {
            return new RequestDescription(_writer.Distributor.EnableDelete());
        }

        public RequestDescription StartDelete()
        {
            return new RequestDescription(_writer.Distributor.StartDelete());
        }

        public RequestDescription AddDbModule(DbFactory factory)
        {
            var result = _writer.DbModule.AddDbModule(factory.Build());
            return new RequestDescription(result);
        }
    }
}
