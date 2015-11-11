using System.Collections.Generic;
using Qoollo.Client.Request;

namespace Qoollo.Client.WriterGate
{
    public interface IWriterApi
    {
        RequestDescription UpdateModel();

        RequestDescription Restore(bool isModelUpdated);

        RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated);

        RequestDescription Restore(bool isModelUpdated, string tableName);

        RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated, string tableName);

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
