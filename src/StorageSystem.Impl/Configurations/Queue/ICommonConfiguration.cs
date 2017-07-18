namespace Qoollo.Impl.Configurations.Queue
{
    public interface ICommonConfiguration
    {
        int CountReplics { get; }
        string HashFilename { get; }
        ConnectionConfiguration Connection { get; }
    }

    public class CommonConfiguration : ICommonConfiguration
    {
        public int CountReplics { get; protected set; }
        public string HashFilename { get; protected set; }
        public ConnectionConfiguration Connection { get; protected set; }
    }

    public class ConnectionConfiguration
    {
        public string ServiceName { get; protected set; }
        public int CountConnections { get; protected set; }
        public int TrimPeriod { get; protected set; } // 10000
    }
}