using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Client.Support;

namespace Qoollo.Client.WriterGate
{
    public interface IWriterApi
    {
        RequestDescription UpdateModel();

        RequestDescription Restore(RestoreMode mode);

        RequestDescription Restore(List<ServerAddress> servers, RestoreMode mode);

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
