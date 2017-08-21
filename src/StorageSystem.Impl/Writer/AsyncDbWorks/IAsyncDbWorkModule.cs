using System.Collections.Generic;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Timeout;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal interface IAsyncDbWorkModule
    {
        bool IsRestoreStarted { get; }
        RestoreState RestoreState { get; }
        TimeoutModule TimeoutModule { get; }

        GetRestoreStateResult GetWriterState(SetRestoreStateCommand command);
        GetRestoreStateResult GetWriterState();
        List<ServerId> GetFailedServers();
        void LastMessageIncome(ServerId server);
        void PeriodMessageIncome(ServerId server);
        void Restore(RestoreFromDistributorCommand comm);
        void Restore(RestoreCommand comm);
        void Restore(RestoreState state, RestoreType type, List<ServerId> destServers);
        void RestoreIncome(ServerId server, RestoreState state, string tableName);
        void UpdateModel();
    }
}