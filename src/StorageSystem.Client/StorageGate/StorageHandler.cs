using System.Collections.Generic;
using System.Linq;
using Qoollo.Client.Request;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;

namespace Qoollo.Client.StorageGate
{
    internal class StorageHandler: IStorageApi
    {
        private DbControllerSystem _dbController;

        public StorageHandler(DbControllerSystem dbController)
        {
            _dbController = dbController;
        }

        public void UpdateModel()
        {
            _dbController.Distributor.UpdateModel();
        }

        public RequestDescription Restore(ServerAddress server, bool isModelUpdated)
        {
            string result = _dbController.Distributor.Restore(new ServerId( server.Host, server.Port), isModelUpdated);
            return new RequestDescription(result);
        }

        public RequestDescription Restore(ServerAddress server, List<ServerAddress> servers, bool isModelUpdated)
        {
            var list = new List<ServerId>();
            servers.ForEach(x => list.Add(new ServerId(x.Host, x.Port)));
            string result = _dbController.Distributor.Restore(new ServerId(server.Host, server.Port), list,
                isModelUpdated);
            return new RequestDescription(result);
        }

        public RequestDescription Restore(ServerAddress server, bool isModelUpdated, string tableName)
        {
            string result = _dbController.Distributor.Restore(new ServerId(server.Host, server.Port), isModelUpdated,
                tableName);
            return new RequestDescription(result);
        }

        public RequestDescription Restore(ServerAddress server, List<ServerAddress> servers, bool isModelUpdated, string tableName)
        {
            var list = new List<ServerId>();
            servers.ForEach(x => list.Add(new ServerId(x.Host, x.Port)));
            string result = _dbController.Distributor.Restore(new ServerId(server.Host, server.Port), list,
                isModelUpdated, tableName);
            return new RequestDescription(result);
        }

        public bool IsRestoreCompleted()
        {
            return _dbController.Distributor.IsRestoreCompleted();
        }

        public List<ServerAddress> FailedServers()
        {
            return
                _dbController.Distributor.FailedServers().Select(x => new ServerAddress(x.RemoteHost, x.Port)).ToList();
        }

        public RequestDescription InitDb()
        {
            var result = _dbController.DbModule.InitDb();
            return new RequestDescription(result);
        }

        public RequestDescription InitDb(string name)
        {
            var result = _dbController.DbModule.InitDb(name);
            return new RequestDescription(result);
        }

        public RequestDescription AddDbModule(DbFactory factory)
        {
            var result = _dbController.DbModule.AddDbModule(factory.Build());
            return new RequestDescription(result);
        }
    }
}
