using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Client.Support;

namespace Qoollo.Client.WriterGate
{
    public interface IWriterApi
    {
        RequestDescription UpdateModel();

        RequestDescription Restore(RestoreMode mode);

        RequestDescription Restore(RestoreMode mode, RestoreType type);

        RequestDescription Restore(List<ServerAddress> servers, RestoreMode mode, RestoreType type);


        bool IsRestoreCompleted();

        List<ServerAddress> FailedServers();

        string GetAllState();

        RequestDescription InitDb();

        RequestDescription InitDb(string name);

        RequestDescription DisableDelete();

        RequestDescription EnableDelete();

        RequestDescription StartDelete();

        RequestDescription RunDelete();
    }
}
