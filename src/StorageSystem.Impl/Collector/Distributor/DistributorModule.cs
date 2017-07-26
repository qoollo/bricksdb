using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Collector.CollectorNet;
using Qoollo.Impl.Collector.Interfaces;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.System.Collector;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Interfaces;

namespace Qoollo.Impl.Collector.Distributor
{
    internal class DistributorModule : ControlModule, IDistributorModule
    {
        private ICollectorModel _model;
        private ICollectorNetModule _collectorNet;
        private IAsyncTaskModule _asyncTaskModule;
        private ICollectorConfiguration _config;

        public DistributorModule(StandardKernel kernel) : base(kernel)
        {
        }

        public override void Start()
        {
            _model = Kernel.Get<ICollectorModel>();
            _asyncTaskModule = Kernel.Get<IAsyncTaskModule>();
            _collectorNet = Kernel.Get<ICollectorNetModule>();

            _config = Kernel.Get<ICollectorConfiguration>();

            if (_model.UseStart)
            {
                _model.Start();
                _asyncTaskModule.AddAsyncTask(
                    new AsyncDataPeriod(_config.Timeouts.ServersPingMls.PeriodTimeSpan,
                        PingProcess, AsyncTasksNames.AsyncPing, -1), false);
            }
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
                        new AsyncDataPeriod(_config.Timeouts.ServersPingMls.PeriodTimeSpan,
                            PingProcess, AsyncTasksNames.AsyncPing, -1), false);

                _asyncTaskModule.AddAsyncTask(
                    new AsyncDataPeriod(_config.Timeouts.DistributorUpdateHashMls.PeriodTimeSpan,
                        data => GetHashInfo(server), AsyncTasksNames.GetHashFromDistributor, -1), false);
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
            _collectorNet.PingWriter(servers, _model.ServerAvailable);
        }
    }
}
