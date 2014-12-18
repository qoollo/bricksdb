using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.CollectorNet;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.System.Collector;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;

namespace Qoollo.Impl.Collector.Distributor
{
    internal class DistributorModule : ControlModule
    {
        private readonly CollectorModel _model;
        private CollectorNetModule _collectorNet;
        private readonly AsyncTaskModule _asyncTaskModule;
        private readonly AsyncTasksConfiguration _asyncPing;

        public DistributorModule(CollectorModel model,  AsyncTaskModule asyncTaskModule,
            AsyncTasksConfiguration asyncPing)
        {
            _model = model;
            _asyncTaskModule = asyncTaskModule;
            _asyncPing = asyncPing;
        }

        public void SetNetModule(CollectorNetModule collectorNet)
        {
            _collectorNet = collectorNet;
        }

        public List<ServerId> GetAvailableServers()
        {
            return _model.GetAvailableServers();
        }

        public void ServerUnavailable(ServerId server)
        {
            _model.ServerNotAvailable(server);
        }

        public SystemSearchStateInner GetState()
        {
            return _model.GetSystemState();
        }

        public override void Start()
        {
            if (_model.UseStart)
            {
                _model.Start();
                _asyncTaskModule.AddAsyncTask(
                    new AsyncDataPeriod(_asyncPing.TimeoutPeriod, PingProcess, AsyncTasksNames.AsyncPing, -1), false);
            }
        }

        public string SayIAmHere(ServerId server)
        {
            if (!_model.UseStart)
            {
                if (!_collectorNet.ConnectToDistributor(server))
                    return new ServerNotAvailable(server).Description;

                var ret = _collectorNet.SendSyncToDistributor(server, new GetHashMapCommand());

                if (!(ret is HashMapResult))
                    return ret.Description;

                var hash = ret as HashMapResult;

                _model.NewServers(hash.Servers);

                if (!_model.UseStart)
                    _asyncTaskModule.AddAsyncTask(
                        new AsyncDataPeriod(_asyncPing.TimeoutPeriod, PingProcess, AsyncTasksNames.AsyncPing, -1), false);

                _asyncTaskModule.AddAsyncTask(
                    new AsyncDataPeriod(TimeSpan.FromMinutes(1), data => GetHashInfo(server),
                        AsyncTasksNames.GetHashFromDistributor, -1), false);
            }

            return "";
        }

        private void GetHashInfo(ServerId server)
        {
            var ret = _collectorNet.SendSyncToDistributor(server, new GetHashMapCommand());

            if (!(ret is HashMapResult))
                return;

            var hash = ret as HashMapResult;

            _model.NewServers(hash.Servers);
        }

        private void PingProcess(AsyncData data)
        {
            var servers = _collectorNet.GetServersByType(typeof(SingleConnectionToDistributor));
            _collectorNet.PingDistributors(servers);

            servers = _model.GetAllServers2();
            _collectorNet.PingDbControllers(servers, _model.ServerAvailable);
        }
    }
}
