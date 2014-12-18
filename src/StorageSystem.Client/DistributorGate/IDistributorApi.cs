using Qoollo.Client.Request;

namespace Qoollo.Client.DistributorGate
{
    public interface IDistributorApi
    {
        RequestDescription SayIAmHere(string host, int port);
    }
}
