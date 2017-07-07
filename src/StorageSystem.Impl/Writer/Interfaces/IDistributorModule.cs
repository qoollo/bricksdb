using System.Collections.Generic;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.Interfaces
{
    internal interface IDistributorModule
    {
        string UpdateModel();

        string Restore();

        string Restore(RestoreState state);

        string Restore(RestoreState state, RestoreType type);

        string Restore(RestoreType type);

        string Restore(List<ServerId> servers, RestoreState state, RestoreType type = RestoreType.Single);

        bool IsRestoreCompleted();

        List<ServerId> FailedServers();

        RestoreState GetRestoreRequiredState();

        string GetAllState();

        string DisableDelete();

        string EnableDelete();

        string StartDelete();

        string RunDelete();

        TResult Execute<TValue, TResult>(TValue value)
            where TValue : class;
    }
}