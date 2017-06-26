using System;
using System.Collections.Generic;
using System.Linq;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
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

        public RequestDescription Restore()
        {
            string result = _writer.Distributor.Restore();
            return new RequestDescription(result);
        }


        public RequestDescription Restore(RestoreMode mode)
        {
            string result = _writer.Distributor.Restore(RestoreModeConverter.Convert(mode));
            return new RequestDescription(result);
        }

        public RequestDescription Restore(List<ServerAddress> servers, RestoreMode mode)
        {
            var list = new List<ServerId>();
            servers.ForEach(x => list.Add(new ServerId(x.Host, x.Port)));
            var result = _writer.Distributor.Restore(list, RestoreModeConverter.Convert(mode));
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

        public RequestDescription RunDelete()
        {
            return new RequestDescription(_writer.Distributor.RunDelete());
        }

        public RequestDescription AddDbModule(DbFactory factory)
        {
            var result = _writer.DbModule.AddDbModule(factory.Build());
            return new RequestDescription(result);
        }
    }
}
