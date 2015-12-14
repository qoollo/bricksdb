using System.Collections.Generic;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;

namespace Qoollo.Client.DistributorGate
{
    public interface IDistributorApi
    {
        List<ServerAddress> GetDistributors();

        RequestDescription UpdateModel();

        RequestDescription SayIAmHere(string host, int port);

        string GetServersState();

        RequestDescription AutoRestoreSetMode(bool mode);

        RequestDescription Restore(ServerAddress restoreServer, ServerAddress remoteRestoreServer, RestoreMode mode);

        RequestDescription Delete(string mode);
    }
}
