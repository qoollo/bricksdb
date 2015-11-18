using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Client.WriterGate
{
    public interface IWriterApi
    {
        RequestDescription UpdateModel();

        RequestDescription Restore();

        RequestDescription Restore(RestoreMode mode);

        RequestDescription Restore(List<ServerAddress> servers, RestoreMode mode);

        RequestDescription Restore(RestoreMode mode, string tableName);

        RequestDescription Restore(List<ServerAddress> servers, RestoreMode mode, string tableName);

        bool IsRestoreCompleted();

        List<ServerAddress> FailedServers();

        string GetAllState();

        RequestDescription InitDb();

        RequestDescription InitDb(string name);

        RequestDescription DisableDelete();

        RequestDescription EnableDelete();

        RequestDescription StartDelete();
    }
}
