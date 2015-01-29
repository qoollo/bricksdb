using System.Collections.Generic;
using Qoollo.Client.Request;

namespace Qoollo.Client.WriterGate
{
    public interface IWriterApi
    {
        void UpdateModel();

        RequestDescription Restore(ServerAddress server, bool isModelUpdated);

        RequestDescription Restore(ServerAddress server, List<ServerAddress> servers, bool isModelUpdated);

        RequestDescription Restore(ServerAddress server, bool isModelUpdated, string tableName);

        RequestDescription Restore(ServerAddress server, List<ServerAddress> servers, bool isModelUpdated,
            string tableName);

        bool IsRestoreCompleted();

        List<ServerAddress> FailedServers();

        RequestDescription InitDb();

        RequestDescription InitDb(string name);
    }
}
