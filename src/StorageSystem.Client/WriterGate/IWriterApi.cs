using System.Collections.Generic;
using Qoollo.Client.Request;

namespace Qoollo.Client.WriterGate
{
    public interface IWriterApi
    {
        void UpdateModel();

        RequestDescription Restore(bool isModelUpdated);

        RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated);

        RequestDescription Restore(bool isModelUpdated, string tableName);

        RequestDescription Restore(List<ServerAddress> servers, bool isModelUpdated, string tableName);

        bool IsRestoreCompleted();

        List<ServerAddress> FailedServers();

        RequestDescription InitDb();

        RequestDescription InitDb(string name);
    }
}
