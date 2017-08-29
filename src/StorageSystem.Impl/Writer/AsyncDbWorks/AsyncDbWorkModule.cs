using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Writer.AsyncDbWorks.Restore;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Timeout;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class AsyncDbWorkModule : ControlModule, IAsyncDbWorkModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public RestoreState RestoreState => _serversController.WriterState;

        public TimeoutModule TimeoutModule => _timeout;

        internal bool IsNeedRestore => _serversController.IsNeedRestore();

        public bool IsRestoreStarted => _initiatorRestore.IsStart || _broadcastRestore.IsStart;

        public AsyncDbWorkModule(StandardKernel kernel): base(kernel)
        {
        }

        private IWriterModel _writerModel;

        private BroadcastRestoreModule _broadcastRestore;
        private InitiatorRestoreModule _initiatorRestore;
        private TransferRestoreModule _transferRestore;
        private TimeoutModule _timeout;

        private RestoreProcessController _serversController;

        public override void Start()
        {
            _writerModel = Kernel.Get<IWriterModel>();

            LoadRestoreStateFromFile(Kernel.Get<IWriterConfiguration>().RestoreStateFilename);

            _initiatorRestore = new InitiatorRestoreModule(Kernel, _serversController);
            _transferRestore = new TransferRestoreModule(Kernel);
            _timeout = new TimeoutModule(Kernel);
            _broadcastRestore = new BroadcastRestoreModule(Kernel);

            _initiatorRestore.Start();
            _transferRestore.Start();
            _broadcastRestore.Start();

            _timeout.Start();

            if (_serversController.IsNeedRestore())
            {
                Task.Delay(Consts.StartRestoreTimeout).ContinueWith(task =>
                {
                    //Todo broadcast and check old
                    RestoreFromFile(_serversController.Servers);
                });
            }
        }

        public void UpdateModel()
        {
            _serversController.UpdateModel(_writerModel.Servers);
        }

        #region Restore process
        
        public void RestoreIncome(ServerId server, RestoreState state, string tableName)
        {
            _transferRestore.Restore(server, state == RestoreState.FullRestoreNeed, tableName);
        }

        public void PeriodMessageIncome(ServerId server)
        {
            _initiatorRestore.PeriodMessageIncome(server);
        }
        
        public void LastMessageIncome(ServerId server)
        {            
            _initiatorRestore.LastMessageIncome(server);
        }

        private void LoadRestoreStateFromFile(string filename)
        {
            var saver = new WriterStateFileLogger(filename);
            saver.Load();
            _serversController = new RestoreProcessController(saver);
        }

        #endregion

        #region Restore start 

        public void Restore(RestoreFromDistributorCommand comm)
        {
            var state = comm.RestoreState;
            var type = comm.Type;
            var destServers = comm.Server == null ? null : new List<ServerId> {comm.Server};

            Restore(state, type, destServers);
        }

        public void Restore(RestoreCommand comm)
        {
            var state = comm.RestoreState;
            var type = comm.Type;
            var destServers = comm.DirectServers;

            Restore(state, type, destServers);
        }

        public void Restore(RestoreState state, RestoreType type, List<ServerId> destServers)
        {
            if (_logger.IsWarnEnabled)
                _logger.Warn(
                    $"Attempt to start restore state: {Enum.GetName(typeof(RestoreState), state)}, type: {Enum.GetName(typeof(RestoreType), type)}",
                    "restore");

            if (IsRestoreStarted)
            {
                if (_logger.IsWarnEnabled)
                    _logger.Warn("Cant run restore. Restore in progress", "restore");
                return;
            }

            if (state == RestoreState.Restored)
            {
                if (_logger.IsWarnEnabled)
                    _logger.Warn(
                        $"Cant run restore in {Enum.GetName(typeof(RestoreState), RestoreState.Restored)} state",
                        "restore");
                return;
            }

            var servers = state == RestoreState.FullRestoreNeed
                ? _writerModel.Servers
                : _writerModel.OtherServers;

            if (destServers != null)
            {
                RestoreRun(ServersOnDirectRestore(servers, destServers), state, type);
            }

            if (type == RestoreType.Single)
            {
                RestoreRun(ConvertRestoreServers(servers), state, type);
            }
            else if (type == RestoreType.Broadcast)
            {
                RestoreRun(ConvertRestoreServers(_writerModel.Servers), state, type);
            }
        }

        private void RestoreRun(List<RestoreServer> servers, RestoreState state, RestoreType type)
        {
            if (type == RestoreType.Single)
            {
                if (_initiatorRestore.IsStart)
                    return;

                _serversController.SetRestoreDate(state, type, servers);
                _initiatorRestore.Restore(state, Consts.AllTables);
            }
            if (type == RestoreType.Broadcast)
            {
                if (_broadcastRestore.IsStart)
                    return;

                _serversController.SetRestoreDate(state, type, servers);
                _broadcastRestore.Restore(servers, state);                
            }
        }

        private void RestoreFromFile(List<RestoreServer> servers)
        {
            if (_initiatorRestore.IsStart)
                return;

            _serversController.SetRestoreDate(_serversController.WriterState, RestoreType.Single, servers);
            _initiatorRestore.RestoreFromFile(_serversController.WriterState, Consts.AllTables);
        }

        private List<RestoreServer> ServersOnDirectRestore(List<ServerId> servers, List<ServerId> failedServers)
        {
            return servers.Select(x =>
            {
                var ret = new RestoreServer(x, _writerModel.GetHashMap(x));
                if (failedServers.Contains(x))
                    ret.NeedRestoreInitiate();
                return ret;
            }).ToList();
        }

        private List<RestoreServer> ConvertRestoreServers(IEnumerable<ServerId> servers)
        {
            return servers.Select(x =>
            {
                var ret = new RestoreServer(x, _writerModel.GetHashMap(x));
                ret.NeedRestoreInitiate();
                return ret;
            }).ToList();
        }

        #endregion

        #region Support

        public List<ServerId> GetFailedServers()
        {
            return _initiatorRestore.FailedServers;
        }

        public GetRestoreStateResult GetWriterState(SetRestoreStateCommand command)
        {
            _serversController.DistributorSendState(command.State, command.UpdateState);
            return GetWriterState();
        }

        public GetRestoreStateResult GetWriterState()
        {
            var result = new GetRestoreStateResult(_initiatorRestore.GetState(), _transferRestore.GetState(),
                _broadcastRestore.GetState(), _serversController.GetState());
            return result;
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {                
                _broadcastRestore.Dispose();
                _transferRestore.Dispose();
                _initiatorRestore.Dispose();
                _timeout.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}
