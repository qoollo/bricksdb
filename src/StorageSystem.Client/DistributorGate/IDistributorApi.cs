using Qoollo.Client.Request;

namespace Qoollo.Client.DistributorGate
{
    public interface IDistributorApi
    {
        RequestDescription GetDistributors();

        void UpdateModel();

        RequestDescription SayIAmHere(string host, int port);

        RequestDescription GetServersState();
    }
}
