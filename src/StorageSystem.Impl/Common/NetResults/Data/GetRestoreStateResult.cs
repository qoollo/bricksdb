using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.Data
{
    [DataContract]
    internal class GetRestoreStateResult : SuccessResult
    {
        [DataMember]
        public InitiatorStateDataContainer InitiatorState { get; private set; }

        [DataMember]
        public TransferStateDataContainer TransferState { get; private set; }

        [DataMember]
        public BroadcastStateDataContainer BroadcastState { get; private set; }

        [DataMember]
        public WriterStateDataContainer WriterState { get; private set; }

        public GetRestoreStateResult(InitiatorStateDataContainer initiatorState,
            TransferStateDataContainer transferState, BroadcastStateDataContainer broadcastState,
            WriterStateDataContainer writerState)
        {
            InitiatorState = initiatorState;
            TransferState = transferState;
            BroadcastState = broadcastState;
            WriterState = writerState;
        }
    }

    [DataContract]
    internal class TransferStateDataContainer
    {
        [DataMember]
        public ServerId TransferServer { get; private set; }

        [DataMember]
        public DateTime StartedTime { get; private set; }

        public TransferStateDataContainer(ServerId transferServer, DateTime startedTime)
        {
            TransferServer = transferServer;
            StartedTime = startedTime;
        }
    }

    [DataContract]
    internal class InitiatorStateDataContainer
    {
        [DataMember]
        public ServerId CurrentServer { get; private set; }

        public InitiatorStateDataContainer(ServerId currentServer)
        {
            CurrentServer = currentServer;
        }
    }

    [DataContract]
    internal class BroadcastStateDataContainer
    {
        [DataMember]
        public DateTime StartedTime { get; private set; }

        public BroadcastStateDataContainer(DateTime startedTime)
        {
            StartedTime = startedTime;
        }
    }

    [DataContract]
    internal class WriterStateDataContainer
    {
        [DataMember]
        public RestoreState State { get; private set; }

        [DataMember]
        public List<RestoreServer> RestoreServers { get; private set; }

        public WriterStateDataContainer(RestoreState state, List<RestoreServer> restoreServers)
        {
            State = state;
            RestoreServers = restoreServers;
        }
    }
}
