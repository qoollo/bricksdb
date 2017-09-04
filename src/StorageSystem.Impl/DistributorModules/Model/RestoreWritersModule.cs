using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ninject;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;

namespace Qoollo.Impl.DistributorModules.Model
{
    internal class RestoreWritersModule:ControlModule
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private readonly WriterSystemModel _writerModel;
        private readonly AsyncTaskModule _asyncTask;
        private bool _autoRestoreEnable;
        private IDistributorNetModule _distributorNet;

        public RestoreWritersModule(StandardKernel kernel, WriterSystemModel writerModel, AsyncTaskModule asyncTask)
            : base(kernel)
        {
            _writerModel = writerModel;
            _asyncTask = asyncTask;
        }

        public override void Start()
        {
            _distributorNet = Kernel.Get<IDistributorNetModule>();
            var config = Kernel.Get<IDistributorConfiguration>();
            _autoRestoreEnable = config.AutoRestoreEnable;

            _asyncTask.AddAsyncTask(
                new AsyncDataPeriod(config.Timeouts.CheckRestoreMls.PeriodTimeSpan, UpdateStateAndRunRestore,
                    AsyncTasksNames.CheckRestore, -1), false);
        }

        private void UpdateStateAndRunRestore(AsyncData obj)
        {
            UpdateStateAndRunRestoreInner();
        }

        private void UpdateStateAndRunRestoreInner()
        {
            UpdateState();
            RestoreServers();
        }

        private void UpdateState()
        {
            var servers = _writerModel.GetAllAvailableServers();

            foreach (var writer in servers)
            {
                var state = writer.RestoreState;
                List<ServerId> writerServers = null;
                switch (state)
                {
                    case RestoreState.SimpleRestoreNeed:
                        writerServers = _writerModel.GetAllServers2();
                        break;
                    case RestoreState.FullRestoreNeed:
                        writerServers = _writerModel.GetAllServersExcept(writer);
                        break;
                }

                var result = _distributorNet.SendToWriter(writer,
                    new SetRestoreStateCommand(state, writerServers, writer.WriterUpdateState));

                if (result is GetRestoreStateResult)
                {
                    writer.UpdateState((GetRestoreStateResult)result);
                }
            }
        }

        private void RestoreServers()
        {
            var servers = _writerModel.GetAllAvailableServers();
            if (_autoRestoreEnable)
                RestoreWriters(servers);
        }

        private void RestoreWriters(List<WriterDescription> servers)
        {
            if (!servers.All(x => x.IsAvailable && !x.IsRestoreInProcess))
                return;
            var server = servers.FirstOrDefault(x => x.RestoreState == RestoreState.SimpleRestoreNeed);
            if (server != null)
            {
                _distributorNet.SendToWriter(server,
                    new RestoreFromDistributorCommand(RestoreState.SimpleRestoreNeed,
                        _writerModel.GetAllServersExcept(server)));
            }
        }

        public void AutoRestoreSetMode(bool mode)
        {
            _lock.EnterWriteLock();
            _autoRestoreEnable = mode;
            _lock.ExitWriteLock();
        }

        public string GetServersState()
        {
            UpdateState();
            return _writerModel.Servers.Aggregate(string.Empty,
                (current, writerDescription) => current + "\n" + writerDescription.StateString);
        }

        public string Restore(ServerId server, ServerId restoreDest, RestoreState state)
        {
            if (!_writerModel.Servers.Contains(server))
                return $"Server {server} is unknown";

            var firstOrDefault = _writerModel.Servers.FirstOrDefault(x => Equals(x, server));
            if (state == RestoreState.Restored && firstOrDefault.RestoreState == RestoreState.Restored)
                return $"Cannot run restore in {Enum.GetName(typeof (RestoreState), RestoreState.Restored)} mode";
            var result = _distributorNet.SendToWriter(server, new RestoreFromDistributorCommand(state, restoreDest));

            return result.IsError ? result.ToString() : Errors.NoErrors;
        }

        private string GetServersList(List<RestoreServer> servers, string start = "\n")
        {
            return servers.Aggregate(start, (current, server) => current + $"\t{server}\n");
        }
    }
}